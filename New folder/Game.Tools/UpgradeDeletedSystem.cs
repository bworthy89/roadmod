using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class UpgradeDeletedSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpgradeDeletedJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Lot> m_LotData;

		[ReadOnly]
		public ComponentLookup<Clear> m_ClearAreaData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> m_PrefabBuildingTerraformData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeList<ClearAreaData> clearAreas = default(NativeList<ClearAreaData>);
			NativeList<ClearAreaData> clearAreas2 = default(NativeList<ClearAreaData>);
			NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				if (m_DeletedData.HasComponent(owner.m_Owner) || !m_TransformData.TryGetComponent(owner.m_Owner, out var componentData))
				{
					continue;
				}
				Transform transform = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				UpdateGarageLanes(unfilteredChunkIndex, owner.m_Owner);
				if (m_SubAreas.TryGetBuffer(entity, out var bufferData))
				{
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					ClearAreaHelpers.FillClearAreas(bufferData, transform, objectGeometryData, Entity.Null, ref m_ClearAreaData, ref m_AreaNodes, ref m_AreaTriangles, ref clearAreas);
					ClearAreaHelpers.InitClearAreas(clearAreas, componentData);
				}
				if (clearAreas.IsEmpty)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, owner.m_Owner, default(Updated));
					continue;
				}
				if (m_InstalledUpgrades.TryGetBuffer(owner.m_Owner, out var bufferData2))
				{
					ClearAreaHelpers.FillClearAreas(bufferData2, entity, m_TransformData, m_ClearAreaData, m_PrefabRefData, m_PrefabObjectGeometryData, m_SubAreas, m_AreaNodes, m_AreaTriangles, ref clearAreas2);
					ClearAreaHelpers.InitClearAreas(clearAreas2, componentData);
				}
				PrefabRef prefabRef2 = m_PrefabRefData[owner.m_Owner];
				if (m_PrefabSubNets.TryGetBuffer(prefabRef2.m_Prefab, out var bufferData3))
				{
					NativeList<float4> nodePositions = new NativeList<float4>(bufferData3.Length * 2, Allocator.Temp);
					BuildingUtils.LotInfo lotInfo;
					bool ownerLot = GetOwnerLot(owner.m_Owner, out lotInfo);
					for (int j = 0; j < bufferData3.Length; j++)
					{
						Game.Prefabs.SubNet subNet = bufferData3[j];
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
					for (int k = 0; k < nodePositions.Length; k++)
					{
						nodePositions[k] /= math.max(1f, nodePositions[k].w);
					}
					for (int l = 0; l < bufferData3.Length; l++)
					{
						Game.Prefabs.SubNet subNet2 = NetUtils.GetSubNet(bufferData3, l, m_LefthandTraffic, ref m_PrefabNetGeometryData);
						CreateSubNet(subNet2.m_Prefab, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, componentData, owner.m_Owner, clearAreas, clearAreas2, lotInfo, ownerLot, unfilteredChunkIndex, ref random);
					}
					nodePositions.Dispose();
				}
				if (m_PrefabSubAreas.TryGetBuffer(prefabRef2.m_Prefab, out var bufferData4))
				{
					DynamicBuffer<SubAreaNode> dynamicBuffer = m_PrefabSubAreaNodes[prefabRef2.m_Prefab];
					if (m_SubAreas.TryGetBuffer(owner.m_Owner, out var bufferData5))
					{
						for (int m = 0; m < bufferData5.Length; m++)
						{
							Entity area = bufferData5[m].m_Area;
							PrefabRef prefabRef3 = m_PrefabRefData[area];
							if (m_PrefabSpawnableObjectData.HasComponent(prefabRef3))
							{
								if (!selectedSpawnables.IsCreated)
								{
									selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
								}
								selectedSpawnables.TryAdd(item: (!m_PseudoRandomSeedData.TryGetComponent(area, out var componentData2)) ? random.NextInt() : componentData2.m_Seed, key: prefabRef3);
							}
						}
					}
					for (int n = 0; n < bufferData4.Length; n++)
					{
						Game.Prefabs.SubArea subArea = bufferData4[n];
						int seed;
						if (m_PrefabPlaceholderElements.TryGetBuffer(subArea.m_Prefab, out var bufferData6))
						{
							if (!selectedSpawnables.IsCreated)
							{
								selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
							}
							if (!AreaUtils.SelectAreaPrefab(bufferData6, m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
							{
								continue;
							}
						}
						else
						{
							seed = random.NextInt();
						}
						if (m_PrefabAreaGeometryData[subArea.m_Prefab].m_Type != AreaType.Space || !ClearAreaHelpers.ShouldClear(clearAreas, dynamicBuffer, subArea.m_NodeRange, componentData) || ClearAreaHelpers.ShouldClear(clearAreas2, dynamicBuffer, subArea.m_NodeRange, componentData))
						{
							continue;
						}
						Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
						CreationDefinition component = new CreationDefinition
						{
							m_Prefab = subArea.m_Prefab,
							m_RandomSeed = seed,
							m_Owner = owner.m_Owner,
							m_Flags = CreationFlags.Permanent
						};
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, component);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Updated));
						DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_CommandBuffer.AddBuffer<Game.Areas.Node>(unfilteredChunkIndex, e);
						dynamicBuffer2.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
						int num = ObjectToolBaseSystem.GetFirstNodeIndex(dynamicBuffer, subArea.m_NodeRange);
						int num2 = 0;
						for (int num3 = subArea.m_NodeRange.x; num3 <= subArea.m_NodeRange.y; num3++)
						{
							float3 position = dynamicBuffer[num].m_Position;
							float3 position2 = ObjectUtils.LocalToWorld(componentData, position);
							int parentMesh = dynamicBuffer[num].m_ParentMesh;
							float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
							dynamicBuffer2[num2] = new Game.Areas.Node(position2, elevation);
							num2++;
							if (++num == subArea.m_NodeRange.y)
							{
								num = subArea.m_NodeRange.x;
							}
						}
					}
				}
				UpdateObject(unfilteredChunkIndex, owner.m_Owner);
				clearAreas.Clear();
				if (clearAreas2.IsCreated)
				{
					clearAreas2.Clear();
				}
				if (selectedSpawnables.IsCreated)
				{
					selectedSpawnables.Clear();
				}
			}
			if (clearAreas.IsCreated)
			{
				clearAreas.Dispose();
			}
			if (clearAreas2.IsCreated)
			{
				clearAreas2.Dispose();
			}
			if (selectedSpawnables.IsCreated)
			{
				selectedSpawnables.Dispose();
			}
		}

		private void UpdateGarageLanes(int jobIndex, Entity entity)
		{
			if (m_DeletedData.HasComponent(entity))
			{
				return;
			}
			if (m_SubLanes.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Game.Net.SubLane subLane = bufferData[i];
					if ((subLane.m_PathMethods & (PathMethod.Parking | PathMethod.BicycleParking)) != 0)
					{
						m_CommandBuffer.AddComponent(jobIndex, subLane.m_SubLane, default(PathfindUpdated));
					}
				}
			}
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					UpdateGarageLanes(jobIndex, bufferData2[j].m_SubObject);
				}
			}
		}

		private void UpdateObject(int jobIndex, Entity entity)
		{
			if (m_DeletedData.HasComponent(entity))
			{
				return;
			}
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					UpdateObject(jobIndex, bufferData[i].m_SubObject);
				}
			}
		}

		private void CreateSubNet(Entity netPrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, Transform ownerTransform, Entity owner, NativeList<ClearAreaData> removedClearAreas, NativeList<ClearAreaData> remainingClearAreas, BuildingUtils.LotInfo lotInfo, bool hasLot, int jobIndex, ref Random random)
		{
			m_PrefabNetGeometryData.TryGetComponent(netPrefab, out var componentData);
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = netPrefab,
				m_RandomSeed = random.NextInt(),
				m_Owner = owner,
				m_Flags = CreationFlags.Permanent
			};
			bool flag = parentMesh.x >= 0 && parentMesh.y >= 0;
			NetCourse component2 = default(NetCourse);
			if ((componentData.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
			{
				curve.y = default(Bezier4x1);
				Curve curve2 = new Curve
				{
					m_Bezier = ObjectUtils.LocalToWorld(ownerTransform.m_Position, ownerTransform.m_Rotation, curve)
				};
				component2.m_Curve = NetUtils.AdjustPosition(curve2, fixedStart: false, linearMiddle: false, fixedEnd: false, ref m_TerrainHeightData, ref m_WaterSurfaceData).m_Bezier;
			}
			else if (!flag)
			{
				Curve curve3 = new Curve
				{
					m_Bezier = ObjectUtils.LocalToWorld(ownerTransform.m_Position, ownerTransform.m_Rotation, curve)
				};
				bool flag2 = parentMesh.x >= 0;
				bool flag3 = parentMesh.y >= 0;
				flag = flag2 || flag3;
				if ((componentData.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0)
				{
					if (hasLot)
					{
						component2.m_Curve = NetUtils.AdjustPosition(curve3, flag2, flag, flag3, ref lotInfo).m_Bezier;
						component2.m_Curve.a.y += curve.a.y;
						component2.m_Curve.b.y += curve.b.y;
						component2.m_Curve.c.y += curve.c.y;
						component2.m_Curve.d.y += curve.d.y;
					}
					else
					{
						component2.m_Curve = curve3.m_Bezier;
					}
				}
				else
				{
					component2.m_Curve = NetUtils.AdjustPosition(curve3, flag2, flag, flag3, ref m_TerrainHeightData).m_Bezier;
					component2.m_Curve.a.y += curve.a.y;
					component2.m_Curve.b.y += curve.b.y;
					component2.m_Curve.c.y += curve.c.y;
					component2.m_Curve.d.y += curve.d.y;
				}
			}
			else
			{
				component2.m_Curve = ObjectUtils.LocalToWorld(ownerTransform.m_Position, ownerTransform.m_Rotation, curve);
			}
			bool onGround = !flag || math.cmin(math.abs(curve.y.abcd)) < 2f;
			if (!ClearAreaHelpers.ShouldClear(removedClearAreas, component2.m_Curve, onGround) || ClearAreaHelpers.ShouldClear(remainingClearAreas, component2.m_Curve, onGround))
			{
				return;
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex);
			m_CommandBuffer.AddComponent(jobIndex, e, component);
			m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
			component2.m_StartPosition.m_Position = component2.m_Curve.a;
			component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), ownerTransform.m_Rotation);
			component2.m_StartPosition.m_CourseDelta = 0f;
			component2.m_StartPosition.m_Elevation = curve.a.y;
			component2.m_StartPosition.m_ParentMesh = parentMesh.x;
			if (nodeIndex.x >= 0)
			{
				if ((componentData.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
				{
					component2.m_StartPosition.m_Position.xz = ObjectUtils.LocalToWorld(ownerTransform, nodePositions[nodeIndex.x].xyz).xz;
				}
				else
				{
					component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(ownerTransform, nodePositions[nodeIndex.x].xyz);
				}
			}
			component2.m_EndPosition.m_Position = component2.m_Curve.d;
			component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), ownerTransform.m_Rotation);
			component2.m_EndPosition.m_CourseDelta = 1f;
			component2.m_EndPosition.m_Elevation = curve.d.y;
			component2.m_EndPosition.m_ParentMesh = parentMesh.y;
			if (nodeIndex.y >= 0)
			{
				if ((componentData.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
				{
					component2.m_EndPosition.m_Position.xz = ObjectUtils.LocalToWorld(ownerTransform, nodePositions[nodeIndex.y].xyz).xz;
				}
				else
				{
					component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(ownerTransform, nodePositions[nodeIndex.y].xyz);
				}
			}
			component2.m_Length = MathUtils.Length(component2.m_Curve);
			component2.m_FixedIndex = -1;
			component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
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

		private bool GetOwnerLot(Entity lotOwner, out BuildingUtils.LotInfo lotInfo)
		{
			if (m_LotData.TryGetComponent(lotOwner, out var componentData) && m_TransformData.TryGetComponent(lotOwner, out var componentData2) && m_PrefabRefData.TryGetComponent(lotOwner, out var componentData3) && m_PrefabBuildingData.TryGetComponent(componentData3.m_Prefab, out var componentData4))
			{
				float2 extents = new float2(componentData4.m_LotSize) * 4f;
				m_ElevationData.TryGetComponent(lotOwner, out var componentData5);
				m_InstalledUpgrades.TryGetBuffer(lotOwner, out var bufferData);
				lotInfo = BuildingUtils.CalculateLotInfo(extents, componentData2, componentData5, componentData, componentData3, bufferData, m_TransformData, m_PrefabRefData, m_PrefabObjectGeometryData, m_PrefabBuildingTerraformData, m_PrefabBuildingExtensionData, defaultNoSmooth: false, out var _);
				return true;
			}
			lotInfo = default(BuildingUtils.LotInfo);
			return false;
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Lot> __Game_Buildings_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Clear> __Game_Areas_Clear_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Buildings_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Lot>(isReadOnly: true);
			__Game_Areas_Clear_RO_ComponentLookup = state.GetComponentLookup<Clear>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup = state.GetComponentLookup<BuildingTerraformData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_DeletedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Transform>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_DeletedQuery);
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpgradeDeletedJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ClearAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Clear_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingTerraformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_RandomSeed = RandomSeed.Next(),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_DeletedQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
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
	public UpgradeDeletedSystem()
	{
	}
}
