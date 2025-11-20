using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BuildingConstructionSystem : GameSystemBase
{
	[BurstCompile]
	private struct BuildingConstructionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<UnderConstruction> m_UnderConstructionType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Crane> m_CraneData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<CraneData> m_PrefabCraneData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<MeshBatch> m_MeshBatches;

		[ReadOnly]
		public BufferLookup<MeshColor> m_MeshColors;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		[ReadOnly]
		public BufferLookup<ColorVariation> m_PrefabColorVariations;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PointOfInterest> m_PointOfInterest;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public bool m_DebugFastSpawn;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeParallelHashMap<Entity, Entity>.ParallelWriter m_PreviousPrefabMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<UnderConstruction> nativeArray2 = chunk.GetNativeArray(ref m_UnderConstructionType);
			NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Transform transform = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				ref UnderConstruction reference = ref nativeArray2.ElementAt(i);
				if (reference.m_Progress < 100)
				{
					if (reference.m_Speed == 0)
					{
						reference.m_Speed = (byte)random.NextInt(39, 89);
					}
					if (m_DebugFastSpawn)
					{
						reference.m_Progress = 100;
						continue;
					}
					if (reference.m_Progress == 0)
					{
						reference.m_Progress++;
						UpdateCranes(ref random, entity, transform, prefabRef);
						continue;
					}
					uint num = (m_SimulationFrame >> 6) + reference.m_Speed;
					uint num2 = (uint)((ulong)((long)num * (long)reference.m_Speed) >> 7);
					uint num3 = (uint)((ulong)((long)(num + 1) * (long)reference.m_Speed) >> 7);
					reference.m_Progress = (byte)math.min(255, (int)(reference.m_Progress + (num3 - num2)));
					if (random.NextInt(10) == 0)
					{
						UpdateCranes(ref random, entity, transform, prefabRef);
					}
				}
				else
				{
					if (reference.m_NewPrefab == Entity.Null)
					{
						reference.m_NewPrefab = prefabRef.m_Prefab;
					}
					UpdatePrefab(unfilteredChunkIndex, entity, reference.m_NewPrefab, transform, ref random, ref selectedSpawnables);
					m_CommandBuffer.RemoveComponent<UnderConstruction>(unfilteredChunkIndex, entity);
					m_PreviousPrefabMap.TryAdd(entity, prefabRef.m_Prefab);
				}
			}
			if (selectedSpawnables.IsCreated)
			{
				selectedSpawnables.Dispose();
			}
		}

		private void UpdateCranes(ref Random random, Entity entity, Transform transform, PrefabRef prefabRef)
		{
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			if (!m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				if (!m_CraneData.HasComponent(subObject))
				{
					continue;
				}
				Transform transform2 = m_TransformData[subObject];
				PrefabRef prefabRef2 = m_PrefabRefData[subObject];
				float3 position = random.NextFloat3(objectGeometryData.m_Bounds.min, objectGeometryData.m_Bounds.max);
				position = ObjectUtils.LocalToWorld(transform, position);
				if (m_PrefabCraneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData))
				{
					position = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(transform2), position);
					float num = math.length(position.xz);
					if (num < componentData.m_DistanceRange.min)
					{
						position.xz = math.normalizesafe(position.xz, new float2(0f, 1f)) * componentData.m_DistanceRange.min;
					}
					else if (num > componentData.m_DistanceRange.max)
					{
						position.xz = math.normalizesafe(position.xz, new float2(0f, 1f)) * componentData.m_DistanceRange.max;
					}
					position = ObjectUtils.LocalToWorld(transform2, position);
				}
				PointOfInterest value = m_PointOfInterest[subObject];
				value.m_Position = position;
				value.m_IsValid = true;
				m_PointOfInterest[subObject] = value;
			}
		}

		private void UpdatePrefab(int jobIndex, Entity entity, Entity newPrefab, Transform transform, ref Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			m_CommandBuffer.SetComponent(jobIndex, entity, new PrefabRef(newPrefab));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
			if (m_MeshBatches.TryGetBuffer(entity, out var bufferData))
			{
				DynamicBuffer<MeshBatch> dynamicBuffer = m_CommandBuffer.SetBuffer<MeshBatch>(jobIndex, entity);
				dynamicBuffer.ResizeUninitialized(bufferData.Length);
				for (int i = 0; i < bufferData.Length; i++)
				{
					MeshBatch value = bufferData[i];
					value.m_MeshGroup = byte.MaxValue;
					value.m_MeshIndex = byte.MaxValue;
					value.m_TileIndex = byte.MaxValue;
					dynamicBuffer[i] = value;
				}
			}
			bool flag = false;
			if (m_PrefabSubMeshes.TryGetBuffer(newPrefab, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					if (m_PrefabColorVariations.HasBuffer(bufferData2[j].m_SubMesh))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag != m_MeshColors.HasBuffer(entity))
			{
				if (flag)
				{
					m_CommandBuffer.AddBuffer<MeshColor>(jobIndex, entity);
				}
				else
				{
					m_CommandBuffer.RemoveComponent<MeshColor>(jobIndex, entity);
				}
			}
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData3))
			{
				for (int k = 0; k < bufferData3.Length; k++)
				{
					Entity subObject = bufferData3[k].m_SubObject;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(Updated));
				}
			}
			if (m_SubAreas.TryGetBuffer(entity, out var bufferData4))
			{
				for (int l = 0; l < bufferData4.Length; l++)
				{
					m_CommandBuffer.AddComponent<Deleted>(jobIndex, bufferData4[l].m_Area);
				}
			}
			if (m_PrefabSubAreas.TryGetBuffer(newPrefab, out var bufferData5))
			{
				if (!bufferData4.IsCreated)
				{
					m_CommandBuffer.AddBuffer<Game.Areas.SubArea>(jobIndex, entity);
				}
				if (selectedSpawnables.IsCreated)
				{
					selectedSpawnables.Clear();
				}
				CreateAreas(jobIndex, entity, transform, bufferData5, m_PrefabSubAreaNodes[newPrefab], ref random, ref selectedSpawnables);
			}
			else if (bufferData4.IsCreated)
			{
				m_CommandBuffer.RemoveComponent<Game.Areas.SubArea>(jobIndex, entity);
			}
			if (m_SubNets.TryGetBuffer(entity, out var bufferData6))
			{
				for (int m = 0; m < bufferData6.Length; m++)
				{
					Game.Net.SubNet subNet = bufferData6[m];
					bool flag2 = true;
					if (m_ConnectedEdges.TryGetBuffer(subNet.m_SubNet, out var bufferData7))
					{
						for (int n = 0; n < bufferData7.Length; n++)
						{
							Entity edge = bufferData7[n].m_Edge;
							if ((!m_OwnerData.TryGetComponent(edge, out var componentData) || (!(componentData.m_Owner == entity) && !m_DeletedData.HasComponent(componentData.m_Owner))) && !m_DeletedData.HasComponent(edge))
							{
								Edge edge2 = m_EdgeData[edge];
								if (edge2.m_Start == subNet.m_SubNet || edge2.m_End == subNet.m_SubNet)
								{
									flag2 = false;
								}
								m_CommandBuffer.AddComponent(jobIndex, edge, default(Updated));
								if (!m_DeletedData.HasComponent(edge2.m_Start))
								{
									m_CommandBuffer.AddComponent(jobIndex, edge2.m_Start, default(Updated));
								}
								if (!m_DeletedData.HasComponent(edge2.m_End))
								{
									m_CommandBuffer.AddComponent(jobIndex, edge2.m_End, default(Updated));
								}
							}
						}
					}
					if (flag2)
					{
						m_CommandBuffer.AddComponent<Deleted>(jobIndex, subNet.m_SubNet);
						continue;
					}
					m_CommandBuffer.RemoveComponent<Owner>(jobIndex, subNet.m_SubNet);
					m_CommandBuffer.AddComponent(jobIndex, subNet.m_SubNet, default(Updated));
				}
			}
			if (m_PrefabSubNets.HasBuffer(newPrefab))
			{
				if (bufferData6.IsCreated)
				{
					m_CommandBuffer.SetBuffer<Game.Net.SubNet>(jobIndex, entity);
				}
				else
				{
					m_CommandBuffer.AddBuffer<Game.Net.SubNet>(jobIndex, entity);
				}
				CreateNets(jobIndex, entity, transform, m_PrefabSubNets[newPrefab], ref random);
			}
			else if (bufferData6.IsCreated)
			{
				m_CommandBuffer.RemoveComponent<Game.Net.SubNet>(jobIndex, entity);
			}
		}

		private void CreateAreas(int jobIndex, Entity owner, Transform transform, DynamicBuffer<Game.Prefabs.SubArea> subAreas, DynamicBuffer<SubAreaNode> subAreaNodes, ref Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			for (int i = 0; i < subAreas.Length; i++)
			{
				Game.Prefabs.SubArea subArea = subAreas[i];
				int seed;
				if (m_PrefabPlaceholderElements.TryGetBuffer(subArea.m_Prefab, out var bufferData))
				{
					if (!selectedSpawnables.IsCreated)
					{
						selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
					}
					if (!AreaUtils.SelectAreaPrefab(bufferData, m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
					{
						continue;
					}
				}
				else
				{
					seed = random.NextInt();
				}
				Entity e = m_CommandBuffer.CreateEntity(jobIndex);
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = subArea.m_Prefab,
					m_Owner = owner,
					m_RandomSeed = seed
				};
				component.m_Flags |= CreationFlags.Permanent;
				m_CommandBuffer.AddComponent(jobIndex, e, component);
				m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
				DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_CommandBuffer.AddBuffer<Game.Areas.Node>(jobIndex, e);
				dynamicBuffer.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
				int num = ObjectToolBaseSystem.GetFirstNodeIndex(subAreaNodes, subArea.m_NodeRange);
				int num2 = 0;
				for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
				{
					float3 position = subAreaNodes[num].m_Position;
					float3 position2 = ObjectUtils.LocalToWorld(transform, position);
					int parentMesh = subAreaNodes[num].m_ParentMesh;
					float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
					dynamicBuffer[num2] = new Game.Areas.Node(position2, elevation);
					num2++;
					if (++num == subArea.m_NodeRange.y)
					{
						num = subArea.m_NodeRange.x;
					}
				}
			}
		}

		private void CreateNets(int jobIndex, Entity owner, Transform transform, DynamicBuffer<Game.Prefabs.SubNet> subNets, ref Random random)
		{
			NativeList<float4> nodePositions = new NativeList<float4>(subNets.Length * 2, Allocator.Temp);
			for (int i = 0; i < subNets.Length; i++)
			{
				Game.Prefabs.SubNet subNet = subNets[i];
				if (subNet.m_NodeIndex.x >= 0)
				{
					while (nodePositions.Length <= subNet.m_NodeIndex.x)
					{
						nodePositions.Add(default(float4));
					}
					nodePositions[subNet.m_NodeIndex.x] += new float4(subNet.m_Curve.a, 1f);
				}
				if (subNet.m_NodeIndex.y >= 0)
				{
					while (nodePositions.Length <= subNet.m_NodeIndex.y)
					{
						nodePositions.Add(default(float4));
					}
					nodePositions[subNet.m_NodeIndex.y] += new float4(subNet.m_Curve.d, 1f);
				}
			}
			for (int j = 0; j < nodePositions.Length; j++)
			{
				nodePositions[j] /= math.max(1f, nodePositions[j].w);
			}
			for (int k = 0; k < subNets.Length; k++)
			{
				Game.Prefabs.SubNet subNet2 = NetUtils.GetSubNet(subNets, k, m_LefthandTraffic, ref m_PrefabNetGeometryData);
				CreateSubNet(jobIndex, subNet2.m_Prefab, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, owner, transform, ref random);
			}
			nodePositions.Dispose();
		}

		private void CreateSubNet(int jobIndex, Entity netPrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, Entity owner, Transform transform, ref Random random)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex);
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = netPrefab,
				m_Owner = owner,
				m_RandomSeed = random.NextInt()
			};
			component.m_Flags |= CreationFlags.Permanent;
			m_CommandBuffer.AddComponent(jobIndex, e, component);
			m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
			NetCourse component2 = default(NetCourse);
			component2.m_Curve = ObjectUtils.LocalToWorld(transform.m_Position, transform.m_Rotation, curve);
			component2.m_StartPosition.m_Position = component2.m_Curve.a;
			component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), transform.m_Rotation);
			component2.m_StartPosition.m_CourseDelta = 0f;
			component2.m_StartPosition.m_Elevation = curve.a.y;
			component2.m_StartPosition.m_ParentMesh = parentMesh.x;
			if (nodeIndex.x >= 0)
			{
				component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(transform.m_Position, transform.m_Rotation, nodePositions[nodeIndex.x].xyz);
			}
			component2.m_EndPosition.m_Position = component2.m_Curve.d;
			component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), transform.m_Rotation);
			component2.m_EndPosition.m_CourseDelta = 1f;
			component2.m_EndPosition.m_Elevation = curve.d.y;
			component2.m_EndPosition.m_ParentMesh = parentMesh.y;
			if (nodeIndex.y >= 0)
			{
				component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(transform.m_Position, transform.m_Rotation, nodePositions[nodeIndex.y].xyz);
			}
			component2.m_Length = MathUtils.Length(component2.m_Curve);
			component2.m_FixedIndex = -1;
			component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.DisableMerge;
			component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast | CoursePosFlags.DisableMerge;
			if (component2.m_StartPosition.m_Position.Equals(component2.m_EndPosition.m_Position))
			{
				component2.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				component2.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			m_CommandBuffer.AddComponent(jobIndex, e, component2);
			if (upgrades != default(CompositionFlags))
			{
				Upgraded component3 = new Upgraded
				{
					m_Flags = upgrades
				};
				m_CommandBuffer.AddComponent(jobIndex, e, component3);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<UnderConstruction> __Game_Objects_UnderConstruction_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Crane> __Game_Objects_Crane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CraneData> __Game_Prefabs_CraneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ColorVariation> __Game_Prefabs_ColorVariation_RO_BufferLookup;

		public ComponentLookup<PointOfInterest> __Game_Common_PointOfInterest_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnderConstruction>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Objects_Crane_RO_ComponentLookup = state.GetComponentLookup<Crane>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_CraneData_RO_ComponentLookup = state.GetComponentLookup<CraneData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Rendering_MeshBatch_RO_BufferLookup = state.GetBufferLookup<MeshBatch>(isReadOnly: true);
			__Game_Rendering_MeshColor_RO_BufferLookup = state.GetBufferLookup<MeshColor>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_ColorVariation_RO_BufferLookup = state.GetBufferLookup<ColorVariation>(isReadOnly: true);
			__Game_Common_PointOfInterest_RW_ComponentLookup = state.GetComponentLookup<PointOfInterest>();
		}
	}

	private const int UPDATE_INTERVAL_BITS = 6;

	public const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private ZoneSpawnSystem m_ZoneSpawnSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_BuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_ZoneSpawnSystem = base.World.GetOrCreateSystemManaged<ZoneSpawnSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<UnderConstruction>(), ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new BuildingConstructionJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnderConstructionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UnderConstruction_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CraneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Crane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCraneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CraneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshBatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshBatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_MeshColors = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabColorVariations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ColorVariation_RO_BufferLookup, ref base.CheckedStateRef),
			m_PointOfInterest = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PointOfInterest_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_DebugFastSpawn = m_ZoneSpawnSystem.debugFastSpawn,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PreviousPrefabMap = m_TerrainSystem.GetBuildingUpgradeWriter(m_BuildingQuery.CalculateEntityCountWithoutFiltering())
		}, m_BuildingQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_TerrainSystem.SetBuildingUpgradeWriterDependency(jobHandle);
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
	public BuildingConstructionSystem()
	{
	}
}
