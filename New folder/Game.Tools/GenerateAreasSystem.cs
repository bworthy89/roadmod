using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateAreasSystem : GameSystemBase
{
	private struct OldAreaData : IEquatable<OldAreaData>
	{
		public Entity m_Prefab;

		public Entity m_Original;

		public Entity m_Owner;

		public bool Equals(OldAreaData other)
		{
			if (m_Prefab.Equals(other.m_Prefab) && m_Original.Equals(other.m_Original))
			{
				return m_Owner.Equals(other.m_Owner);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((17 * 31 + m_Prefab.GetHashCode()) * 31 + m_Original.GetHashCode()) * 31 + m_Owner.GetHashCode();
		}
	}

	[BurstCompile]
	private struct CreateAreasJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> m_OwnerDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<LocalNodeCache> m_LocalNodeCacheType;

		[ReadOnly]
		public ComponentLookup<Storage> m_StorageData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AreaData> m_AreaData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_AreaGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> m_LocalNodeCache;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DeletedChunks;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeParallelMultiHashMap<OldAreaData, Entity> deletedAreas = new NativeParallelMultiHashMap<OldAreaData, Entity>(16, Allocator.Temp);
			for (int i = 0; i < m_DeletedChunks.Length; i++)
			{
				FillDeletedAreas(m_DeletedChunks[i], deletedAreas);
			}
			for (int j = 0; j < m_DefinitionChunks.Length; j++)
			{
				CreateAreas(m_DefinitionChunks[j], deletedAreas);
			}
			deletedAreas.Dispose();
		}

		private void FillDeletedAreas(ArchetypeChunk chunk, NativeParallelMultiHashMap<OldAreaData, Entity> deletedAreas)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity item = nativeArray[i];
				OldAreaData key = new OldAreaData
				{
					m_Prefab = nativeArray4[i].m_Prefab,
					m_Original = nativeArray2[i].m_Original
				};
				if (nativeArray3.Length != 0)
				{
					key.m_Owner = nativeArray3[i].m_Owner;
				}
				deletedAreas.Add(key, item);
			}
		}

		private void CreateAreas(ArchetypeChunk chunk, NativeParallelMultiHashMap<OldAreaData, Entity> deletedAreas)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<OwnerDefinition> nativeArray2 = chunk.GetNativeArray(ref m_OwnerDefinitionType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<LocalNodeCache> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LocalNodeCacheType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				if (m_DeletedData.HasComponent(creationDefinition.m_Owner))
				{
					continue;
				}
				OwnerDefinition component = default(OwnerDefinition);
				if (nativeArray2.Length != 0)
				{
					component = nativeArray2[i];
				}
				DynamicBuffer<Node> dynamicBuffer = bufferAccessor[i];
				AreaFlags areaFlags = (AreaFlags)0;
				TempFlags tempFlags = (TempFlags)0u;
				if (creationDefinition.m_Original != Entity.Null)
				{
					m_CommandBuffer.AddComponent(creationDefinition.m_Original, default(Hidden));
					creationDefinition.m_Prefab = m_PrefabRefData[creationDefinition.m_Original].m_Prefab;
					if ((creationDefinition.m_Flags & CreationFlags.Recreate) != 0)
					{
						tempFlags |= TempFlags.Modify;
					}
					else
					{
						areaFlags |= AreaFlags.Complete;
						if ((creationDefinition.m_Flags & CreationFlags.Delete) != 0)
						{
							tempFlags |= TempFlags.Delete;
						}
						else if ((creationDefinition.m_Flags & CreationFlags.Select) != 0)
						{
							tempFlags |= TempFlags.Select;
						}
						else if ((creationDefinition.m_Flags & CreationFlags.Relocate) != 0)
						{
							tempFlags |= TempFlags.Modify;
						}
						else if ((creationDefinition.m_Flags & CreationFlags.Duplicate) != 0)
						{
							tempFlags |= TempFlags.Duplicate;
						}
						if ((creationDefinition.m_Flags & CreationFlags.Parent) != 0)
						{
							tempFlags |= TempFlags.Parent;
						}
					}
				}
				else
				{
					tempFlags |= TempFlags.Create;
				}
				if (component.m_Prefab == Entity.Null)
				{
					tempFlags |= TempFlags.Essential;
				}
				if ((creationDefinition.m_Flags & CreationFlags.Hidden) != 0)
				{
					tempFlags |= TempFlags.Hidden;
				}
				bool flag = false;
				OldAreaData key = new OldAreaData
				{
					m_Prefab = creationDefinition.m_Prefab,
					m_Original = creationDefinition.m_Original,
					m_Owner = creationDefinition.m_Owner
				};
				if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0 && deletedAreas.TryGetFirstValue(key, out var item, out var it))
				{
					deletedAreas.Remove(it);
					m_CommandBuffer.SetComponent(item, new Temp(creationDefinition.m_Original, tempFlags));
					m_CommandBuffer.AddComponent(item, default(Updated));
					m_CommandBuffer.RemoveComponent<Deleted>(item);
					if (component.m_Prefab != Entity.Null)
					{
						m_CommandBuffer.AddComponent(item, default(Owner));
						m_CommandBuffer.AddComponent(item, component);
					}
					else
					{
						if (creationDefinition.m_Owner != Entity.Null)
						{
							m_CommandBuffer.AddComponent(item, new Owner(creationDefinition.m_Owner));
						}
						else
						{
							m_CommandBuffer.RemoveComponent<Owner>(item);
						}
						m_CommandBuffer.RemoveComponent<OwnerDefinition>(item);
					}
					if ((creationDefinition.m_Flags & CreationFlags.Native) != 0 || m_NativeData.HasComponent(creationDefinition.m_Original))
					{
						m_CommandBuffer.AddComponent(item, default(Native));
					}
					else
					{
						m_CommandBuffer.RemoveComponent<Native>(item);
					}
				}
				else
				{
					AreaData areaData = m_AreaData[creationDefinition.m_Prefab];
					item = m_CommandBuffer.CreateEntity(areaData.m_Archetype);
					m_CommandBuffer.SetComponent(item, new PrefabRef(creationDefinition.m_Prefab));
					if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0)
					{
						m_CommandBuffer.AddComponent(item, new Temp(creationDefinition.m_Original, tempFlags));
					}
					if (component.m_Prefab != Entity.Null)
					{
						m_CommandBuffer.AddComponent(item, default(Owner));
						m_CommandBuffer.AddComponent(item, component);
					}
					else if (creationDefinition.m_Owner != Entity.Null)
					{
						m_CommandBuffer.AddComponent(item, new Owner(creationDefinition.m_Owner));
					}
					if ((creationDefinition.m_Flags & CreationFlags.Native) != 0 || m_NativeData.HasComponent(creationDefinition.m_Original))
					{
						m_CommandBuffer.AddComponent(item, default(Native));
					}
					flag = true;
				}
				DynamicBuffer<Node> dynamicBuffer2 = m_CommandBuffer.SetBuffer<Node>(item);
				bool flag2 = false;
				if ((areaFlags & AreaFlags.Complete) == 0 && dynamicBuffer.Length >= 4 && dynamicBuffer[0].m_Position.Equals(dynamicBuffer[dynamicBuffer.Length - 1].m_Position))
				{
					dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length - 1);
					for (int j = 0; j < dynamicBuffer.Length - 1; j++)
					{
						dynamicBuffer2[j] = dynamicBuffer[j];
					}
					areaFlags |= AreaFlags.Complete;
					flag2 = true;
				}
				else
				{
					dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						dynamicBuffer2[k] = dynamicBuffer[k];
					}
				}
				bool flag3 = false;
				bool flag4 = false;
				if (m_AreaGeometryData.TryGetComponent(creationDefinition.m_Prefab, out var componentData))
				{
					flag3 = (componentData.m_Flags & GeometryFlags.OnWaterSurface) != 0;
					flag4 = (componentData.m_Flags & GeometryFlags.PseudoRandom) != 0;
				}
				for (int l = 0; l < dynamicBuffer2.Length; l++)
				{
					ref Node reference = ref dynamicBuffer2.ElementAt(l);
					if (reference.m_Elevation == float.MinValue)
					{
						Node node = ((!flag3) ? AreaUtils.AdjustPosition(reference, ref m_TerrainHeightData) : AreaUtils.AdjustPosition(reference, ref m_TerrainHeightData, ref m_WaterSurfaceData));
						bool test = math.abs(node.m_Position.y - reference.m_Position.y) >= 0.01f;
						reference.m_Position = math.select(reference.m_Position, node.m_Position, test);
					}
				}
				if (bufferAccessor2.Length != 0)
				{
					DynamicBuffer<LocalNodeCache> dynamicBuffer3 = bufferAccessor2[i];
					DynamicBuffer<LocalNodeCache> dynamicBuffer4 = ((!flag && m_LocalNodeCache.HasBuffer(item)) ? m_CommandBuffer.SetBuffer<LocalNodeCache>(item) : m_CommandBuffer.AddBuffer<LocalNodeCache>(item));
					if (flag2)
					{
						dynamicBuffer4.ResizeUninitialized(dynamicBuffer3.Length - 1);
						for (int m = 0; m < dynamicBuffer3.Length - 1; m++)
						{
							dynamicBuffer4[m] = dynamicBuffer3[m];
						}
					}
					else
					{
						dynamicBuffer4.ResizeUninitialized(dynamicBuffer3.Length);
						for (int n = 0; n < dynamicBuffer3.Length; n++)
						{
							dynamicBuffer4[n] = dynamicBuffer3[n];
						}
					}
				}
				else if (!flag && m_LocalNodeCache.HasBuffer(item))
				{
					m_CommandBuffer.RemoveComponent<LocalNodeCache>(item);
				}
				m_CommandBuffer.SetComponent(item, new Area(areaFlags));
				if (m_StorageData.HasComponent(creationDefinition.m_Original))
				{
					m_CommandBuffer.SetComponent(item, m_StorageData[creationDefinition.m_Original]);
				}
				PseudoRandomSeed componentData2 = default(PseudoRandomSeed);
				if (flag4)
				{
					if (!m_PseudoRandomSeedData.TryGetComponent(creationDefinition.m_Original, out componentData2))
					{
						componentData2 = new PseudoRandomSeed((ushort)creationDefinition.m_RandomSeed);
					}
					m_CommandBuffer.SetComponent(item, componentData2);
				}
				if (!m_PrefabSubAreas.TryGetBuffer(creationDefinition.m_Prefab, out var bufferData))
				{
					continue;
				}
				NativeParallelMultiHashMap<Entity, Entity> nativeParallelMultiHashMap = default(NativeParallelMultiHashMap<Entity, Entity>);
				tempFlags = (TempFlags)((uint)tempFlags & 0xFFFFFFF7u);
				areaFlags |= AreaFlags.Slave;
				if (m_SubAreas.TryGetBuffer(creationDefinition.m_Original, out var bufferData2) && bufferData2.Length != 0)
				{
					nativeParallelMultiHashMap = new NativeParallelMultiHashMap<Entity, Entity>(16, Allocator.Temp);
					for (int num = 0; num < bufferData2.Length; num++)
					{
						Game.Areas.SubArea subArea = bufferData2[num];
						nativeParallelMultiHashMap.Add(m_PrefabRefData[subArea.m_Area].m_Prefab, subArea.m_Area);
					}
				}
				for (int num2 = 0; num2 < bufferData.Length; num2++)
				{
					Game.Prefabs.SubArea subArea2 = bufferData[num2];
					key = new OldAreaData
					{
						m_Prefab = subArea2.m_Prefab,
						m_Owner = item
					};
					if (nativeParallelMultiHashMap.IsCreated && nativeParallelMultiHashMap.TryGetFirstValue(subArea2.m_Prefab, out key.m_Original, out var it2))
					{
						nativeParallelMultiHashMap.Remove(it2);
					}
					if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0 && deletedAreas.TryGetFirstValue(key, out var item2, out it))
					{
						deletedAreas.Remove(it);
						m_CommandBuffer.SetComponent(item2, new Temp(key.m_Original, tempFlags));
						m_CommandBuffer.AddComponent(item2, default(Updated));
						m_CommandBuffer.RemoveComponent<Deleted>(item2);
						m_CommandBuffer.AddComponent(item2, new Owner(item));
						if ((creationDefinition.m_Flags & CreationFlags.Native) != 0 || m_NativeData.HasComponent(key.m_Original))
						{
							m_CommandBuffer.AddComponent(item2, default(Native));
						}
						else
						{
							m_CommandBuffer.RemoveComponent<Native>(item2);
						}
					}
					else
					{
						AreaData areaData2 = m_AreaData[subArea2.m_Prefab];
						item2 = m_CommandBuffer.CreateEntity(areaData2.m_Archetype);
						m_CommandBuffer.SetComponent(item2, new PrefabRef(subArea2.m_Prefab));
						if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0)
						{
							m_CommandBuffer.AddComponent(item2, new Temp(key.m_Original, tempFlags));
						}
						m_CommandBuffer.AddComponent(item2, new Owner(item));
						if ((creationDefinition.m_Flags & CreationFlags.Native) != 0 || m_NativeData.HasComponent(key.m_Original))
						{
							m_CommandBuffer.AddComponent(item, default(Native));
						}
					}
					m_CommandBuffer.SetComponent(item2, new Area(areaFlags));
					if (m_StorageData.HasComponent(key.m_Original))
					{
						m_CommandBuffer.SetComponent(item2, m_StorageData[key.m_Original]);
					}
					if (flag4)
					{
						m_CommandBuffer.SetComponent(item2, componentData2);
					}
				}
				if (nativeParallelMultiHashMap.IsCreated)
				{
					nativeParallelMultiHashMap.Dispose();
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> __Game_Tools_OwnerDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Storage> __Game_Areas_Storage_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaData> __Game_Prefabs_AreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OwnerDefinition>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferTypeHandle = state.GetBufferTypeHandle<LocalNodeCache>(isReadOnly: true);
			__Game_Areas_Storage_RO_ComponentLookup = state.GetComponentLookup<Storage>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_AreaData_RO_ComponentLookup = state.GetComponentLookup<AreaData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_DeletedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_DefinitionQuery = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Updated>());
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>());
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> definitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle outJobHandle2;
		NativeList<ArchetypeChunk> deletedChunks = m_DeletedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
		JobHandle deps;
		JobHandle jobHandle = IJobExtensions.Schedule(new CreateAreasJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LocalNodeCacheType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_StorageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Storage_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_LocalNodeCache = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_DefinitionChunks = definitionChunks,
			m_DeletedChunks = deletedChunks,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobUtils.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2, deps));
		definitionChunks.Dispose(jobHandle);
		deletedChunks.Dispose(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
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
	public GenerateAreasSystem()
	{
	}
}
