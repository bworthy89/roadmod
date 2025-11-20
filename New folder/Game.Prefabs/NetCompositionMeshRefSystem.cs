using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class NetCompositionMeshRefSystem : GameSystemBase
{
	private struct NewMeshData
	{
		public Entity m_Entity;

		public CompositionFlags m_Flags;

		public unsafe void* m_Pieces;

		public int m_PieceCount;
	}

	[BurstCompile]
	private struct CompositionMeshRefJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<NetCompositionData> m_CompositionType;

		[ReadOnly]
		public ComponentLookup<MeshData> m_MeshData;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshData> m_CompositionMeshData;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> m_CompositionPieces;

		[ReadOnly]
		public BufferLookup<LodMesh> m_LodMeshes;

		[ReadOnly]
		public BufferLookup<MeshMaterial> m_MeshMaterials;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityArchetype m_MeshArchetype;

		[ReadOnly]
		public NativeParallelMultiHashMap<int, Entity> m_MeshEntities;

		public EntityCommandBuffer m_CommandBuffer;

		public unsafe void Execute()
		{
			NativeParallelMultiHashMap<int, NewMeshData> newMeshes = default(NativeParallelMultiHashMap<int, NewMeshData>);
			NativeList<NetCompositionPiece> tempPieces = default(NativeList<NetCompositionPiece>);
			NativeList<NetCompositionPiece> tempPieces2 = default(NativeList<NetCompositionPiece>);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<NetCompositionData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_CompositionType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity = nativeArray[j];
					NetCompositionData netCompositionData = nativeArray2[j];
					DynamicBuffer<NetCompositionPiece> source = m_CompositionPieces[entity];
					bool hasMesh;
					int hashCode = GetHashCode(netCompositionData.m_Flags, source.AsNativeArray(), out hasMesh);
					if (!hasMesh)
					{
						continue;
					}
					if (TryFindComposition(newMeshes, hashCode, netCompositionData.m_Flags, source.GetUnsafeReadOnlyPtr(), source.Length, ref tempPieces, out var entity2, out var rotate))
					{
						m_CommandBuffer.SetComponent(entity, new NetCompositionMeshRef
						{
							m_Mesh = entity2,
							m_Rotate = rotate
						});
						continue;
					}
					entity2 = m_CommandBuffer.CreateEntity(m_MeshArchetype);
					m_CommandBuffer.SetComponent(entity, new NetCompositionMeshRef
					{
						m_Mesh = entity2
					});
					DynamicBuffer<NetCompositionPiece> dynamicBuffer = m_CommandBuffer.SetBuffer<NetCompositionPiece>(entity2);
					DynamicBuffer<MeshMaterial> materials = m_CommandBuffer.SetBuffer<MeshMaterial>(entity2);
					CopyMeshPieces(source, dynamicBuffer);
					NetCompositionMeshData netCompositionMeshData = new NetCompositionMeshData
					{
						m_Flags = netCompositionData.m_Flags,
						m_Width = netCompositionData.m_Width,
						m_MiddleOffset = netCompositionData.m_MiddleOffset,
						m_HeightRange = netCompositionData.m_HeightRange,
						m_Hash = hashCode
					};
					CalculatePieceData(dynamicBuffer, materials, out netCompositionMeshData.m_DefaultLayers, out netCompositionMeshData.m_State, out netCompositionMeshData.m_IndexFactor, out netCompositionMeshData.m_LodBias, out netCompositionMeshData.m_ShadowBias);
					m_CommandBuffer.SetComponent(entity2, netCompositionMeshData);
					if (!newMeshes.IsCreated)
					{
						newMeshes = new NativeParallelMultiHashMap<int, NewMeshData>(50, Allocator.Temp);
					}
					NewMeshData item = new NewMeshData
					{
						m_Entity = entity2,
						m_Flags = netCompositionData.m_Flags,
						m_Pieces = dynamicBuffer.GetUnsafePtr(),
						m_PieceCount = dynamicBuffer.Length
					};
					newMeshes.Add(hashCode, item);
					InitializeLods(entity2, netCompositionMeshData, dynamicBuffer, newMeshes, ref tempPieces, ref tempPieces2);
				}
			}
			if (newMeshes.IsCreated)
			{
				newMeshes.Dispose();
			}
			if (tempPieces.IsCreated)
			{
				tempPieces.Dispose();
			}
			if (tempPieces2.IsCreated)
			{
				tempPieces2.Dispose();
			}
		}

		private void CopyMeshPieces(DynamicBuffer<NetCompositionPiece> source, DynamicBuffer<NetCompositionPiece> target)
		{
			int num = 0;
			for (int i = 0; i < source.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = source[i];
				num += math.select(0, 1, (netCompositionPiece.m_PieceFlags & NetPieceFlags.HasMesh) != 0 && (netCompositionPiece.m_SectionFlags & NetSectionFlags.Hidden) == 0);
			}
			target.ResizeUninitialized(num);
			num = 0;
			for (int j = 0; j < source.Length; j++)
			{
				NetCompositionPiece value = source[j];
				if ((value.m_PieceFlags & NetPieceFlags.HasMesh) != 0 && (value.m_SectionFlags & NetSectionFlags.Hidden) == 0)
				{
					target[num++] = value;
				}
			}
		}

		private void CalculatePieceData(DynamicBuffer<NetCompositionPiece> pieces, DynamicBuffer<MeshMaterial> materials, out MeshLayer defaultLayers, out MeshFlags meshFlags, out float indexFactor, out float lodBias, out float shadowBias)
		{
			defaultLayers = (MeshLayer)0;
			meshFlags = (MeshFlags)0u;
			indexFactor = 0f;
			lodBias = 0f;
			shadowBias = 0f;
			for (int i = 0; i < pieces.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = pieces[i];
				MeshData meshData = m_MeshData[netCompositionPiece.m_Piece];
				DynamicBuffer<MeshMaterial> dynamicBuffer = m_MeshMaterials[netCompositionPiece.m_Piece];
				defaultLayers |= meshData.m_DefaultLayers;
				meshFlags |= meshData.m_State;
				indexFactor += (float)meshData.m_IndexCount / math.max(1f, MathUtils.Size(meshData.m_Bounds.z));
				lodBias += meshData.m_LodBias;
				shadowBias += meshData.m_ShadowBias;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					int materialIndex = dynamicBuffer[j].m_MaterialIndex;
					int num = 0;
					while (true)
					{
						if (num < materials.Length)
						{
							if (materials[num].m_MaterialIndex == materialIndex)
							{
								break;
							}
							num++;
							continue;
						}
						materials.Add(new MeshMaterial
						{
							m_MaterialIndex = materialIndex
						});
						break;
					}
				}
			}
			if (pieces.Length != 0)
			{
				lodBias /= pieces.Length;
				shadowBias /= pieces.Length;
			}
		}

		private unsafe void InitializeLods(Entity mesh, NetCompositionMeshData meshData, DynamicBuffer<NetCompositionPiece> pieces, NativeParallelMultiHashMap<int, NewMeshData> newMeshes, ref NativeList<NetCompositionPiece> tempPieces, ref NativeList<NetCompositionPiece> tempPieces2)
		{
			int num = 0;
			for (int i = 0; i < pieces.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = pieces[i];
				if (m_LodMeshes.HasBuffer(netCompositionPiece.m_Piece))
				{
					num = math.max(num, m_LodMeshes[netCompositionPiece.m_Piece].Length);
				}
			}
			if (num == 0)
			{
				return;
			}
			DynamicBuffer<LodMesh> dynamicBuffer = m_CommandBuffer.AddBuffer<LodMesh>(mesh);
			dynamicBuffer.ResizeUninitialized(num);
			for (int j = 0; j < num; j++)
			{
				if (tempPieces.IsCreated)
				{
					tempPieces.Clear();
				}
				else
				{
					tempPieces = new NativeList<NetCompositionPiece>(pieces.Length, Allocator.Temp);
				}
				for (int k = 0; k < pieces.Length; k++)
				{
					NetCompositionPiece value = pieces[k];
					if (m_LodMeshes.TryGetBuffer(value.m_Piece, out var bufferData) && bufferData.Length != 0)
					{
						value.m_Piece = bufferData[math.min(j, bufferData.Length - 1)].m_LodMesh;
					}
					tempPieces.Add(in value);
				}
				meshData.m_Hash = GetHashCode(meshData.m_Flags, tempPieces.AsArray(), out var hasMesh);
				if (TryFindComposition(newMeshes, meshData.m_Hash, meshData.m_Flags, tempPieces.GetUnsafeReadOnlyPtr(), tempPieces.Length, ref tempPieces2, out var entity, out hasMesh))
				{
					dynamicBuffer[j] = new LodMesh
					{
						m_LodMesh = entity
					};
					continue;
				}
				entity = m_CommandBuffer.CreateEntity(m_MeshArchetype);
				dynamicBuffer[j] = new LodMesh
				{
					m_LodMesh = entity
				};
				DynamicBuffer<NetCompositionPiece> pieces2 = m_CommandBuffer.SetBuffer<NetCompositionPiece>(entity);
				DynamicBuffer<MeshMaterial> materials = m_CommandBuffer.SetBuffer<MeshMaterial>(entity);
				pieces2.CopyFrom(tempPieces.AsArray());
				CalculatePieceData(pieces2, materials, out meshData.m_DefaultLayers, out meshData.m_State, out meshData.m_IndexFactor, out meshData.m_LodBias, out meshData.m_ShadowBias);
				m_CommandBuffer.SetComponent(entity, meshData);
				NewMeshData item = new NewMeshData
				{
					m_Entity = entity,
					m_Flags = meshData.m_Flags,
					m_Pieces = pieces2.GetUnsafePtr(),
					m_PieceCount = pieces2.Length
				};
				newMeshes.Add(meshData.m_Hash, item);
				InitializeLods(entity, meshData, pieces2, newMeshes, ref tempPieces, ref tempPieces2);
			}
		}

		private unsafe bool TryFindComposition(NativeParallelMultiHashMap<int, NewMeshData> newMeshes, int hash, CompositionFlags flags, void* pieces, int pieceCount, ref NativeList<NetCompositionPiece> tempPieces, out Entity entity, out bool rotate)
		{
			if (m_MeshEntities.TryGetFirstValue(hash, out entity, out var it))
			{
				do
				{
					NetCompositionMeshData netCompositionMeshData = m_CompositionMeshData[entity];
					DynamicBuffer<NetCompositionPiece> dynamicBuffer = m_CompositionPieces[entity];
					if (Equals(flags, netCompositionMeshData.m_Flags, pieces, dynamicBuffer.GetUnsafeReadOnlyPtr(), pieceCount, dynamicBuffer.Length, ref tempPieces, rotate: false))
					{
						rotate = false;
						return true;
					}
					if ((flags.m_General & CompositionFlags.General.Node) == 0 && Equals(flags, netCompositionMeshData.m_Flags, pieces, dynamicBuffer.GetUnsafeReadOnlyPtr(), pieceCount, dynamicBuffer.Length, ref tempPieces, rotate: true))
					{
						rotate = true;
						return true;
					}
				}
				while (m_MeshEntities.TryGetNextValue(out entity, ref it));
			}
			if (newMeshes.IsCreated && newMeshes.TryGetFirstValue(hash, out var item, out it))
			{
				do
				{
					if (Equals(flags, item.m_Flags, pieces, item.m_Pieces, pieceCount, item.m_PieceCount, ref tempPieces, rotate: false))
					{
						entity = item.m_Entity;
						rotate = false;
						return true;
					}
					if ((flags.m_General & CompositionFlags.General.Node) == 0 && Equals(flags, item.m_Flags, pieces, item.m_Pieces, pieceCount, item.m_PieceCount, ref tempPieces, rotate: true))
					{
						entity = item.m_Entity;
						rotate = true;
						return true;
					}
				}
				while (newMeshes.TryGetNextValue(out item, ref it));
			}
			rotate = false;
			return false;
		}

		private int GetHashCode(CompositionFlags flags, NativeArray<NetCompositionPiece> pieces, out bool hasMesh)
		{
			int num = ((uint)GetCompositionFlagMask(flags)).GetHashCode();
			hasMesh = false;
			for (int i = 0; i < pieces.Length; i++)
			{
				NetCompositionPiece netCompositionPiece = pieces[i];
				if ((netCompositionPiece.m_PieceFlags & NetPieceFlags.HasMesh) != 0 && (netCompositionPiece.m_SectionFlags & NetSectionFlags.Hidden) == 0)
				{
					num += netCompositionPiece.m_Piece.GetHashCode();
					hasMesh = true;
				}
			}
			return num;
		}

		private unsafe bool Equals(CompositionFlags flags1, CompositionFlags flags2, void* pieces1, void* pieces2, int pieceCount1, int pieceCount2, ref NativeList<NetCompositionPiece> tempPieces, bool rotate)
		{
			if (GetCompositionFlagMask(flags1) != GetCompositionFlagMask(flags2))
			{
				return false;
			}
			if (tempPieces.IsCreated)
			{
				tempPieces.Clear();
			}
			else
			{
				tempPieces = new NativeList<NetCompositionPiece>(pieceCount2, Allocator.Temp);
			}
			for (int i = 0; i < pieceCount2; i++)
			{
				NetCompositionPiece value = UnsafeUtility.ReadArrayElement<NetCompositionPiece>(pieces2, i);
				if ((value.m_PieceFlags & NetPieceFlags.HasMesh) != 0 && (value.m_SectionFlags & NetSectionFlags.Hidden) == 0)
				{
					tempPieces.Add(in value);
				}
			}
			for (int j = 0; j < pieceCount1; j++)
			{
				NetCompositionPiece piece = UnsafeUtility.ReadArrayElement<NetCompositionPiece>(pieces1, j);
				if ((piece.m_PieceFlags & NetPieceFlags.HasMesh) == 0 || (piece.m_SectionFlags & NetSectionFlags.Hidden) != 0)
				{
					continue;
				}
				NetSectionFlags netSectionFlags = GetSectionFlagMask(piece);
				float3 offset = piece.m_Offset;
				bool flag = false;
				if (rotate)
				{
					if ((netSectionFlags & NetSectionFlags.Left) != 0)
					{
						netSectionFlags &= ~NetSectionFlags.Left;
						netSectionFlags |= NetSectionFlags.Right;
					}
					else if ((netSectionFlags & NetSectionFlags.Right) != 0)
					{
						netSectionFlags &= ~NetSectionFlags.Right;
						netSectionFlags |= NetSectionFlags.Left;
					}
					offset.x = 0f - offset.x;
				}
				for (int k = 0; k < tempPieces.Length; k++)
				{
					NetCompositionPiece piece2 = tempPieces[k];
					if (!(piece.m_Piece != piece2.m_Piece) && netSectionFlags == GetSectionFlagMask(piece2) && !math.any(math.abs(offset - piece2.m_Offset) >= 0.1f))
					{
						NetPieceFlags netPieceFlags = piece.m_PieceFlags | piece2.m_PieceFlags;
						NetSectionFlags netSectionFlags2 = piece.m_SectionFlags ^ piece2.m_SectionFlags;
						bool2 @bool = new bool2((netPieceFlags & NetPieceFlags.AsymmetricMeshX) != 0, (netPieceFlags & NetPieceFlags.AsymmetricMeshZ) != 0);
						bool2 bool2 = new bool2((netSectionFlags2 & NetSectionFlags.Invert) != 0, (netSectionFlags2 & NetSectionFlags.FlipMesh) != 0);
						if (!math.any(@bool & (bool2 != rotate)))
						{
							flag = true;
							tempPieces.RemoveAtSwapBack(k);
							break;
						}
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return tempPieces.Length == 0;
		}

		private CompositionFlags.General GetCompositionFlagMask(CompositionFlags flags)
		{
			return flags.m_General & (CompositionFlags.General.Node | CompositionFlags.General.Roundabout);
		}

		private NetSectionFlags GetSectionFlagMask(NetCompositionPiece piece)
		{
			return piece.m_SectionFlags & (NetSectionFlags.Median | NetSectionFlags.Left | NetSectionFlags.Right | NetSectionFlags.AlignCenter | NetSectionFlags.HalfLength);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionMeshData> __Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> __Game_Prefabs_NetCompositionPiece_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LodMesh> __Game_Prefabs_LodMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshMaterial> __Game_Prefabs_MeshMaterial_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_NetCompositionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionMeshData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionPiece_RO_BufferLookup = state.GetBufferLookup<NetCompositionPiece>(isReadOnly: true);
			__Game_Prefabs_LodMesh_RO_BufferLookup = state.GetBufferLookup<LodMesh>(isReadOnly: true);
			__Game_Prefabs_MeshMaterial_RO_BufferLookup = state.GetBufferLookup<MeshMaterial>(isReadOnly: true);
		}
	}

	private NetCompositionMeshSystem m_NetCompositionMeshSystem;

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_CompositionQuery;

	private EntityArchetype m_MeshArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetCompositionMeshSystem = base.World.GetOrCreateSystemManaged<NetCompositionMeshSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_CompositionQuery = GetEntityQuery(ComponentType.ReadOnly<NetCompositionData>(), ComponentType.ReadOnly<NetCompositionPiece>(), ComponentType.ReadOnly<NetCompositionMeshRef>(), ComponentType.ReadOnly<Created>());
		m_MeshArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<NetCompositionMeshData>(), ComponentType.ReadWrite<NetCompositionPiece>(), ComponentType.ReadWrite<MeshMaterial>(), ComponentType.ReadWrite<BatchGroup>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_CompositionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_CompositionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle dependencies;
		JobHandle jobHandle = IJobExtensions.Schedule(new CompositionMeshRefJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionMeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionPieces = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionPiece_RO_BufferLookup, ref base.CheckedStateRef),
			m_LodMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LodMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshMaterials = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshMaterial_RO_BufferLookup, ref base.CheckedStateRef),
			m_Chunks = chunks,
			m_MeshArchetype = m_MeshArchetype,
			m_MeshEntities = m_NetCompositionMeshSystem.GetMeshEntities(out dependencies),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, dependencies));
		chunks.Dispose(jobHandle);
		m_NetCompositionMeshSystem.AddMeshEntityReader(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public NetCompositionMeshRefSystem()
	{
	}
}
