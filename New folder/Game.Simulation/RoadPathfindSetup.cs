using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct RoadPathfindSetup
{
	[BurstCompile]
	private struct SetupMaintenanceProvidersJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.MaintenanceDepot> m_MaintenanceDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.MaintenanceVehicle> m_MaintenanceVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<MaintenanceDepotData> m_PrefabMaintenanceDepotData;

		[ReadOnly]
		public ComponentLookup<MaintenanceVehicleData> m_PrefabMaintenanceVehicleData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.MaintenanceDepot> nativeArray3 = chunk.GetNativeArray(ref m_MaintenanceDepotType);
			if (nativeArray3.Length != 0)
			{
				for (int i = 0; i < nativeArray3.Length; i++)
				{
					if ((nativeArray3[i].m_Flags & MaintenanceDepotFlags.HasAvailableVehicles) == 0)
					{
						continue;
					}
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity, out var targetSeeker);
						Entity entity2 = nativeArray[i];
						if (AreaUtils.CheckServiceDistrict(entity, entity2, m_ServiceDistricts))
						{
							PrefabRef prefabRef = nativeArray2[i];
							if ((m_PrefabMaintenanceDepotData[prefabRef.m_Prefab].m_MaintenanceType & targetSeeker.m_SetupQueueTarget.m_MaintenanceType) == targetSeeker.m_SetupQueueTarget.m_MaintenanceType)
							{
								float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
								targetSeeker.FindTargets(entity2, cost);
							}
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.MaintenanceVehicle> nativeArray4 = chunk.GetNativeArray(ref m_MaintenanceVehicleType);
			if (nativeArray4.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray5 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray6 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				Game.Vehicles.MaintenanceVehicle maintenanceVehicle = nativeArray4[k];
				PrefabRef prefabRef2 = nativeArray2[k];
				MaintenanceVehicleData maintenanceVehicleData = m_PrefabMaintenanceVehicleData[prefabRef2.m_Prefab];
				if ((maintenanceVehicle.m_State & (MaintenanceVehicleFlags.EstimatedFull | MaintenanceVehicleFlags.Disabled)) != 0)
				{
					continue;
				}
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var targetSeeker2);
					if ((maintenanceVehicleData.m_MaintenanceType & targetSeeker2.m_SetupQueueTarget.m_MaintenanceType) != targetSeeker2.m_SetupQueueTarget.m_MaintenanceType || (nativeArray6.Length != 0 && !AreaUtils.CheckServiceDistrict(entity4, nativeArray6[k].m_Owner, m_ServiceDistricts)))
					{
						continue;
					}
					if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) != 0 || nativeArray5.Length == 0)
					{
						targetSeeker2.FindTargets(entity3, 0f);
						continue;
					}
					PathOwner pathOwner = nativeArray5[k];
					DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor2[k];
					int num = math.min(maintenanceVehicle.m_RequestCount, dynamicBuffer.Length);
					PathElement pathElement = default(PathElement);
					float num2 = 0f;
					bool flag = false;
					if (num >= 1)
					{
						DynamicBuffer<PathElement> dynamicBuffer2 = bufferAccessor[k];
						if (pathOwner.m_ElementIndex < dynamicBuffer2.Length)
						{
							num2 += (float)(dynamicBuffer2.Length - pathOwner.m_ElementIndex) * maintenanceVehicle.m_PathElementTime * targetSeeker2.m_PathfindParameters.m_Weights.time;
							pathElement = dynamicBuffer2[dynamicBuffer2.Length - 1];
							flag = true;
						}
					}
					for (int m = 1; m < num; m++)
					{
						Entity request = dynamicBuffer[m].m_Request;
						if (m_PathInformationData.TryGetComponent(request, out var componentData))
						{
							num2 += componentData.m_Duration * targetSeeker2.m_PathfindParameters.m_Weights.time;
						}
						if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
						{
							pathElement = bufferData[bufferData.Length - 1];
							flag = true;
						}
					}
					if (flag)
					{
						targetSeeker2.m_Buffer.Enqueue(new PathTarget(entity3, pathElement.m_Target, pathElement.m_TargetDelta.y, num2));
					}
					else
					{
						targetSeeker2.FindTargets(entity3, entity3, num2, EdgeFlags.DefaultMask, allowAccessRestriction: true, num >= 1);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SetupRandomTrafficJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<RandomTrafficRequest> m_RandomTrafficRequestData;

		[ReadOnly]
		public ComponentLookup<TrafficSpawnerData> m_PrefabTrafficSpawnerData;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var entity, out var owner, out var targetSeeker);
				Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				RandomTrafficRequest randomTrafficRequest = default(RandomTrafficRequest);
				if (m_RandomTrafficRequestData.HasComponent(owner))
				{
					randomTrafficRequest = m_RandomTrafficRequestData[owner];
				}
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					if (!(entity2 == entity))
					{
						PrefabRef prefabRef = nativeArray2[j];
						TrafficSpawnerData trafficSpawnerData = default(TrafficSpawnerData);
						if (m_PrefabTrafficSpawnerData.HasComponent(prefabRef.m_Prefab))
						{
							trafficSpawnerData = m_PrefabTrafficSpawnerData[prefabRef.m_Prefab];
						}
						if ((randomTrafficRequest.m_RoadType & trafficSpawnerData.m_RoadType) != RoadTypes.None || (randomTrafficRequest.m_TrackType & trafficSpawnerData.m_TrackType) != TrackTypes.None)
						{
							float cost = random.NextFloat(10000f);
							targetSeeker.FindTargets(entity2, cost);
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SetupOutsideConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				float value = targetSeeker.m_SetupQueueTarget.m_Value2;
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					float cost = 0f;
					if (value > 0f)
					{
						cost = random.NextFloat(value);
					}
					targetSeeker.FindTargets(entity2, cost);
					if ((targetSeeker.m_SetupQueueTarget.m_Methods & PathMethod.Flying) == 0 || !targetSeeker.m_Transform.HasComponent(entity2))
					{
						continue;
					}
					float3 position = targetSeeker.m_Transform[entity2].m_Position;
					if ((targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Helicopter) != RoadTypes.None)
					{
						Entity lane = Entity.Null;
						float curvePos = 0f;
						float distance = float.MaxValue;
						targetSeeker.m_AirwayData.helicopterMap.FindClosestLane(position, targetSeeker.m_Curve, ref lane, ref curvePos, ref distance);
						if (lane != Entity.Null)
						{
							targetSeeker.m_Buffer.Enqueue(new PathTarget(entity2, lane, curvePos, cost));
						}
					}
					if ((targetSeeker.m_SetupQueueTarget.m_FlyingTypes & RoadTypes.Airplane) != RoadTypes.None)
					{
						Entity lane2 = Entity.Null;
						float curvePos2 = 0f;
						float distance2 = float.MaxValue;
						targetSeeker.m_AirwayData.airplaneMap.FindClosestLane(position, targetSeeker.m_Curve, ref lane2, ref curvePos2, ref distance2);
						if (lane2 != Entity.Null)
						{
							targetSeeker.m_Buffer.Enqueue(new PathTarget(entity2, lane2, curvePos2, cost));
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct MaintenanceRequestsJob : IJobChunk
	{
		private struct DistrictIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public float2 m_Position;

			public ComponentLookup<District> m_DistrictData;

			public BufferLookup<Game.Areas.Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public Entity m_Result;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Position) && m_DistrictData.HasComponent(areaItem.m_Area))
				{
					DynamicBuffer<Game.Areas.Node> nodes = m_Nodes[areaItem.m_Area];
					DynamicBuffer<Triangle> dynamicBuffer = m_Triangles[areaItem.m_Area];
					if (dynamicBuffer.Length > areaItem.m_Triangle && MathUtils.Intersect(AreaUtils.GetTriangle2(nodes, dynamicBuffer[areaItem.m_Triangle]), m_Position, out var _))
					{
						m_Result = areaItem.m_Area;
					}
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<MaintenanceRequest> m_MaintenanceRequestType;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> m_MaintenanceRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Surface> m_SurfaceData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_ParkData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<NetCondition> m_NetConditionData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetTree;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<MaintenanceRequest> nativeArray3 = chunk.GetNativeArray(ref m_MaintenanceRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_MaintenanceRequestData.TryGetComponent(owner, out var componentData))
				{
					continue;
				}
				Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				Entity service = Entity.Null;
				if (m_VehicleData.HasComponent(componentData.m_Target))
				{
					if (targetSeeker.m_Owner.TryGetComponent(componentData.m_Target, out var componentData2))
					{
						service = componentData2.m_Owner;
					}
				}
				else
				{
					if (!targetSeeker.m_PrefabRef.HasComponent(componentData.m_Target))
					{
						continue;
					}
					service = componentData.m_Target;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					MaintenanceRequest maintenanceRequest = nativeArray3[j];
					MaintenanceType maintenanceType = BuildingUtils.GetMaintenanceType(maintenanceRequest.m_Target, ref m_ParkData, ref m_NetConditionData, ref m_EdgeData, ref m_SurfaceData, ref m_VehicleData);
					if ((maintenanceType & targetSeeker.m_SetupQueueTarget.m_MaintenanceType) != maintenanceType)
					{
						continue;
					}
					float cost = 0f;
					if ((maintenanceType & MaintenanceType.Vehicle) == 0)
					{
						cost = random.NextFloat(30f);
					}
					Entity district = Entity.Null;
					BorderDistrict componentData4;
					Transform componentData5;
					if (m_CurrentDistrictData.TryGetComponent(maintenanceRequest.m_Target, out var componentData3))
					{
						district = componentData3.m_District;
					}
					else if (m_BorderDistrictData.TryGetComponent(maintenanceRequest.m_Target, out componentData4))
					{
						district = componentData4.m_Right;
					}
					else if (targetSeeker.m_Transform.TryGetComponent(maintenanceRequest.m_Target, out componentData5))
					{
						DistrictIterator iterator = new DistrictIterator
						{
							m_Position = componentData5.m_Position.xz,
							m_DistrictData = m_DistrictData,
							m_Nodes = targetSeeker.m_AreaNode,
							m_Triangles = targetSeeker.m_AreaTriangle
						};
						m_AreaTree.Iterate(ref iterator);
						district = iterator.m_Result;
					}
					if (AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
					{
						targetSeeker.FindTargets(nativeArray[j], maintenanceRequest.m_Target, cost, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
						if (targetSeeker.m_Transform.TryGetComponent(maintenanceRequest.m_Target, out var componentData6))
						{
							float num = 30f;
							CommonPathfindSetup.TargetIterator iterator2 = new CommonPathfindSetup.TargetIterator
							{
								m_Entity = nativeArray[j],
								m_Bounds = new Bounds3(componentData6.m_Position - num, componentData6.m_Position + num),
								m_Position = componentData6.m_Position,
								m_MaxDistance = num,
								m_TargetSeeker = targetSeeker,
								m_Flags = EdgeFlags.DefaultMask,
								m_CompositionData = m_CompositionData,
								m_NetCompositionData = m_NetCompositionData
							};
							m_NetTree.Iterate(ref iterator2);
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_MaintenanceProviderQuery;

	private EntityQuery m_RandomTrafficQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_MaintenanceRequestQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

	private ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

	private ComponentTypeHandle<MaintenanceRequest> m_MaintenanceRequestType;

	private ComponentTypeHandle<Game.Buildings.MaintenanceDepot> m_MaintenanceDepotType;

	private ComponentTypeHandle<Game.Vehicles.MaintenanceVehicle> m_MaintenanceVehicleType;

	private ComponentTypeHandle<PrefabRef> m_PrefabRefType;

	private BufferTypeHandle<PathElement> m_PathElementType;

	private BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

	private ComponentLookup<PathInformation> m_PathInformationData;

	private ComponentLookup<RandomTrafficRequest> m_RandomTrafficRequestData;

	private ComponentLookup<MaintenanceRequest> m_MaintenanceRequestData;

	private ComponentLookup<Game.Objects.Surface> m_SurfaceData;

	private ComponentLookup<Game.Buildings.Park> m_ParkData;

	private ComponentLookup<Game.Net.Edge> m_EdgeData;

	private ComponentLookup<NetCondition> m_NetConditionData;

	private ComponentLookup<Composition> m_CompositionData;

	private ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

	private ComponentLookup<BorderDistrict> m_BorderDistrictData;

	private ComponentLookup<District> m_DistrictData;

	private ComponentLookup<Vehicle> m_VehicleData;

	private ComponentLookup<MaintenanceDepotData> m_PrefabMaintenanceDepotData;

	private ComponentLookup<MaintenanceVehicleData> m_PrefabMaintenanceVehicleData;

	private ComponentLookup<TrafficSpawnerData> m_PrefabTrafficSpawnerData;

	private ComponentLookup<NetCompositionData> m_NetCompositionData;

	private BufferLookup<PathElement> m_PathElements;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	public RoadPathfindSetup(PathfindSetupSystem system)
	{
		m_MaintenanceProviderQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.MaintenanceDepot>(),
				ComponentType.ReadOnly<Game.Vehicles.MaintenanceVehicle>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_RandomTrafficQuery = system.GetSetupQuery(ComponentType.ReadOnly<Game.Buildings.TrafficSpawner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_OutsideConnectionQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.OutsideConnection>() },
			None = new ComponentType[5]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Game.Objects.ElectricityOutsideConnection>(),
				ComponentType.ReadOnly<Game.Objects.WaterPipeOutsideConnection>()
			}
		});
		m_MaintenanceRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<MaintenanceRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_EntityType = system.GetEntityTypeHandle();
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_OutsideConnectionType = system.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_ServiceRequestType = system.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
		m_MaintenanceRequestType = system.GetComponentTypeHandle<MaintenanceRequest>(isReadOnly: true);
		m_MaintenanceDepotType = system.GetComponentTypeHandle<Game.Buildings.MaintenanceDepot>(isReadOnly: true);
		m_MaintenanceVehicleType = system.GetComponentTypeHandle<Game.Vehicles.MaintenanceVehicle>(isReadOnly: true);
		m_PrefabRefType = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		m_PathElementType = system.GetBufferTypeHandle<PathElement>(isReadOnly: true);
		m_ServiceDispatchType = system.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
		m_PathInformationData = system.GetComponentLookup<PathInformation>(isReadOnly: true);
		m_RandomTrafficRequestData = system.GetComponentLookup<RandomTrafficRequest>(isReadOnly: true);
		m_MaintenanceRequestData = system.GetComponentLookup<MaintenanceRequest>(isReadOnly: true);
		m_SurfaceData = system.GetComponentLookup<Game.Objects.Surface>(isReadOnly: true);
		m_ParkData = system.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
		m_EdgeData = system.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
		m_NetConditionData = system.GetComponentLookup<NetCondition>(isReadOnly: true);
		m_CompositionData = system.GetComponentLookup<Composition>(isReadOnly: true);
		m_CurrentDistrictData = system.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
		m_BorderDistrictData = system.GetComponentLookup<BorderDistrict>(isReadOnly: true);
		m_DistrictData = system.GetComponentLookup<District>(isReadOnly: true);
		m_VehicleData = system.GetComponentLookup<Vehicle>(isReadOnly: true);
		m_PrefabMaintenanceDepotData = system.GetComponentLookup<MaintenanceDepotData>(isReadOnly: true);
		m_PrefabMaintenanceVehicleData = system.GetComponentLookup<MaintenanceVehicleData>(isReadOnly: true);
		m_PrefabTrafficSpawnerData = system.GetComponentLookup<TrafficSpawnerData>(isReadOnly: true);
		m_NetCompositionData = system.GetComponentLookup<NetCompositionData>(isReadOnly: true);
		m_PathElements = system.GetBufferLookup<PathElement>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_AreaSearchSystem = system.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_NetSearchSystem = system.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
	}

	public JobHandle SetupMaintenanceProviders(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PrefabRefType.Update(system);
		m_MaintenanceDepotType.Update(system);
		m_MaintenanceVehicleType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PathInformationData.Update(system);
		m_PrefabMaintenanceDepotData.Update(system);
		m_PrefabMaintenanceVehicleData.Update(system);
		m_PathElements.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupMaintenanceProvidersJob
		{
			m_EntityType = m_EntityType,
			m_PrefabRefType = m_PrefabRefType,
			m_MaintenanceDepotType = m_MaintenanceDepotType,
			m_MaintenanceVehicleType = m_MaintenanceVehicleType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PathInformationData = m_PathInformationData,
			m_PrefabMaintenanceDepotData = m_PrefabMaintenanceDepotData,
			m_PrefabMaintenanceVehicleData = m_PrefabMaintenanceVehicleData,
			m_PathElements = m_PathElements,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_MaintenanceProviderQuery, inputDeps);
	}

	public JobHandle SetupRandomTraffic(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_PrefabRefType.Update(system);
		m_RandomTrafficRequestData.Update(system);
		m_PrefabTrafficSpawnerData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupRandomTrafficJob
		{
			m_EntityType = m_EntityType,
			m_PrefabRefType = m_PrefabRefType,
			m_RandomTrafficRequestData = m_RandomTrafficRequestData,
			m_PrefabTrafficSpawnerData = m_PrefabTrafficSpawnerData,
			m_SetupData = setupData
		}, m_RandomTrafficQuery, inputDeps);
	}

	public JobHandle SetupOutsideConnections(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_OutsideConnectionType.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupOutsideConnectionsJob
		{
			m_EntityType = m_EntityType,
			m_OutsideConnectionType = m_OutsideConnectionType,
			m_SetupData = setupData
		}, m_OutsideConnectionQuery, inputDeps);
	}

	public JobHandle SetupMaintenanceRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_MaintenanceRequestType.Update(system);
		m_MaintenanceRequestData.Update(system);
		m_SurfaceData.Update(system);
		m_ParkData.Update(system);
		m_EdgeData.Update(system);
		m_NetConditionData.Update(system);
		m_CompositionData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_BorderDistrictData.Update(system);
		m_DistrictData.Update(system);
		m_VehicleData.Update(system);
		m_NetCompositionData.Update(system);
		m_ServiceDistricts.Update(system);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new MaintenanceRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_MaintenanceRequestType = m_MaintenanceRequestType,
			m_MaintenanceRequestData = m_MaintenanceRequestData,
			m_SurfaceData = m_SurfaceData,
			m_ParkData = m_ParkData,
			m_EdgeData = m_EdgeData,
			m_NetConditionData = m_NetConditionData,
			m_CompositionData = m_CompositionData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_BorderDistrictData = m_BorderDistrictData,
			m_DistrictData = m_DistrictData,
			m_VehicleData = m_VehicleData,
			m_NetCompositionData = m_NetCompositionData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
			m_NetTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
			m_SetupData = setupData
		}, m_MaintenanceRequestQuery, JobHandle.CombineDependencies(inputDeps, dependencies, dependencies2));
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		return jobHandle;
	}
}
