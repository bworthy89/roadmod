using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
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

namespace Game.Simulation;

public struct GoodsDeliveryPathfindSetup
{
	[BurstCompile]
	private struct SetupGoodsDeliveryJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<GoodsDeliveryVehicle> m_GoodsDeliveryVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformations;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryRequest> m_GoodsDeliveryRequests;

		[ReadOnly]
		public ComponentLookup<TransportCompanyData> m_TransportCompanyDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElementBufs;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistrictBufs;

		[ReadOnly]
		public BufferLookup<Resources> m_ResourcesBufs;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicleBufs;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElementBufs;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public Entity m_City;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<GoodsDeliveryVehicle> nativeArray2 = chunk.GetNativeArray(ref m_GoodsDeliveryVehicleType);
			Entity entity2;
			if (nativeArray2.Length != 0)
			{
				NativeArray<PathOwner> nativeArray3 = chunk.GetNativeArray(ref m_PathOwnerType);
				BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
				BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					GoodsDeliveryVehicle goodsDeliveryVehicle = nativeArray2[i];
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out entity2, out var _, out var targetSeeker);
						Resource resource = targetSeeker.m_SetupQueueTarget.m_Resource;
						int value = targetSeeker.m_SetupQueueTarget.m_Value;
						float num = 0f;
						int num2 = 0;
						if (m_LayoutElementBufs.HasBuffer(entity) && m_LayoutElementBufs[entity].Length != 0)
						{
							for (int k = 0; k < m_LayoutElementBufs[entity].Length; k++)
							{
								Entity vehicle = m_LayoutElementBufs[entity][k].m_Vehicle;
								if (vehicle != entity && m_DeliveryTrucks.HasComponent(vehicle))
								{
									Game.Vehicles.DeliveryTruck deliveryTruck = m_DeliveryTrucks[vehicle];
									if ((deliveryTruck.m_State & DeliveryTruckFlags.Loaded) != 0 && deliveryTruck.m_Resource == resource && deliveryTruck.m_Amount > 0)
									{
										num2 += deliveryTruck.m_Amount;
									}
								}
							}
						}
						if (m_DeliveryTrucks.TryGetComponent(entity, out var componentData) && componentData.m_Resource == resource)
						{
							num2 += componentData.m_Amount;
						}
						if (num2 <= 0)
						{
							continue;
						}
						if (nativeArray3.Length == 0 && num2 > value)
						{
							targetSeeker.FindTargets(entity, num);
							continue;
						}
						PathOwner pathOwner = nativeArray3[i];
						DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor2[i];
						int length = dynamicBuffer.Length;
						PathElement pathElement = default(PathElement);
						bool flag = false;
						if (length >= 1)
						{
							DynamicBuffer<PathElement> dynamicBuffer2 = bufferAccessor[i];
							if (pathOwner.m_ElementIndex < dynamicBuffer2.Length)
							{
								num += (float)(dynamicBuffer2.Length - pathOwner.m_ElementIndex) * goodsDeliveryVehicle.m_PathElementTime * targetSeeker.m_PathfindParameters.m_Weights.time;
								pathElement = dynamicBuffer2[dynamicBuffer2.Length - 1];
								flag = true;
							}
						}
						int num3 = 0;
						for (int l = 1; l < length; l++)
						{
							Entity request = dynamicBuffer[l].m_Request;
							if (m_PathInformations.TryGetComponent(request, out var componentData2))
							{
								num += componentData2.m_Duration * targetSeeker.m_PathfindParameters.m_Weights.time;
							}
							if (m_PathElementBufs.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
							{
								pathElement = bufferData[bufferData.Length - 1];
								flag = true;
							}
							if (m_GoodsDeliveryRequests.TryGetComponent(request, out var componentData3))
							{
								num3 += componentData3.m_Amount;
							}
						}
						if (value <= num2 - num3)
						{
							if (flag)
							{
								targetSeeker.m_Buffer.Enqueue(new PathTarget(entity, pathElement.m_Target, pathElement.m_TargetDelta.y, num));
							}
							else
							{
								targetSeeker.FindTargets(entity, entity, num, EdgeFlags.DefaultMask, allowAccessRestriction: true, length >= 1);
							}
						}
					}
				}
			}
			else
			{
				if (!chunk.Has<GoodsDeliveryFacility>())
				{
					return;
				}
				for (int m = 0; m < nativeArray.Length; m++)
				{
					Entity entity3 = nativeArray[m];
					Entity prefab = m_PrefabRefs[entity3].m_Prefab;
					for (int n = 0; n < m_SetupData.Length; n++)
					{
						m_SetupData.GetItem(n, out entity2, out var _, out var targetSeeker2);
						Resource resource2 = targetSeeker2.m_SetupQueueTarget.m_Resource;
						m_DeliveryTruckSelectData.GetCapacityRange(resource2, out var _, out var _);
						int resources = EconomyUtils.GetResources(resource2, m_ResourcesBufs[entity3]);
						int value2 = targetSeeker2.m_SetupQueueTarget.m_Value;
						if (resources <= 0 || resources < value2)
						{
							continue;
						}
						if ((targetSeeker2.m_SetupQueueTarget.m_Flags & SetupTargetFlags.RequireTransport) != SetupTargetFlags.None)
						{
							if (!m_TransportCompanyDatas.HasComponent(prefab))
							{
								continue;
							}
							TransportCompanyData transportCompanyData = m_TransportCompanyDatas[prefab];
							int num4 = 0;
							if (m_OwnedVehicleBufs.HasBuffer(entity3))
							{
								num4 = m_OwnedVehicleBufs[entity3].Length;
							}
							if (num4 >= transportCompanyData.m_MaxTransports - 1)
							{
								continue;
							}
						}
						float num5 = targetSeeker2.m_PathfindParameters.m_Weights.time * 10f;
						if (resources < value2 * 3)
						{
							num5 += 1000f;
						}
						targetSeeker2.FindTargets(entity3, num5);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_GoodsDeliveryFacilityQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<GoodsDeliveryVehicle> m_GoodsDeliveryVehicleType;

	private BufferTypeHandle<PathElement> m_PathElementType;

	private BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

	private ComponentLookup<PathInformation> m_PathInformations;

	private ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

	private ComponentLookup<GoodsDeliveryRequest> m_GoodsDeliveryRequests;

	private ComponentLookup<TransportCompanyData> m_TransportCompanyDatas;

	private ComponentLookup<PrefabRef> m_PrefabRefs;

	private ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

	private ComponentLookup<Game.City.City> m_CityData;

	private BufferLookup<PathElement> m_PathElementBufs;

	private BufferLookup<ServiceDistrict> m_ServiceDistrictBufs;

	private BufferLookup<Resources> m_ResourcesBufs;

	private BufferLookup<OwnedVehicle> m_OwnedVehicleBufs;

	private BufferLookup<LayoutElement> m_LayoutElementBufs;

	private CitySystem m_CitySystem;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	public GoodsDeliveryPathfindSetup(PathfindSetupSystem system)
	{
		m_GoodsDeliveryFacilityQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<GoodsDeliveryFacility>(),
				ComponentType.ReadOnly<GoodsDeliveryVehicle>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_EntityType = system.GetEntityTypeHandle();
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_GoodsDeliveryVehicleType = system.GetComponentTypeHandle<GoodsDeliveryVehicle>(isReadOnly: true);
		m_PathElementType = system.GetBufferTypeHandle<PathElement>(isReadOnly: true);
		m_ServiceDispatchType = system.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
		m_PathInformations = system.GetComponentLookup<PathInformation>(isReadOnly: true);
		m_DeliveryTrucks = system.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
		m_GoodsDeliveryRequests = system.GetComponentLookup<GoodsDeliveryRequest>(isReadOnly: true);
		m_TransportCompanyDatas = system.GetComponentLookup<TransportCompanyData>(isReadOnly: true);
		m_PrefabRefs = system.GetComponentLookup<PrefabRef>(isReadOnly: true);
		m_OutsideConnections = system.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_CityData = system.GetComponentLookup<Game.City.City>(isReadOnly: true);
		m_PathElementBufs = system.GetBufferLookup<PathElement>(isReadOnly: true);
		m_ServiceDistrictBufs = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_ResourcesBufs = system.GetBufferLookup<Resources>(isReadOnly: true);
		m_OwnedVehicleBufs = system.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
		m_LayoutElementBufs = system.GetBufferLookup<LayoutElement>(isReadOnly: true);
		m_CitySystem = system.World.GetOrCreateSystemManaged<CitySystem>();
		m_VehicleCapacitySystem = system.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
	}

	public JobHandle SetupGoodsDelivery(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_GoodsDeliveryVehicleType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PathInformations.Update(system);
		m_DeliveryTrucks.Update(system);
		m_GoodsDeliveryRequests.Update(system);
		m_TransportCompanyDatas.Update(system);
		m_PrefabRefs.Update(system);
		m_PathElementBufs.Update(system);
		m_ServiceDistrictBufs.Update(system);
		m_OutsideConnections.Update(system);
		m_ResourcesBufs.Update(system);
		m_OwnedVehicleBufs.Update(system);
		m_LayoutElementBufs.Update(system);
		m_CityData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupGoodsDeliveryJob
		{
			m_EntityType = m_EntityType,
			m_GoodsDeliveryVehicleType = m_GoodsDeliveryVehicleType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PathInformations = m_PathInformations,
			m_DeliveryTrucks = m_DeliveryTrucks,
			m_GoodsDeliveryRequests = m_GoodsDeliveryRequests,
			m_TransportCompanyDatas = m_TransportCompanyDatas,
			m_PrefabRefs = m_PrefabRefs,
			m_PathElementBufs = m_PathElementBufs,
			m_ServiceDistrictBufs = m_ServiceDistrictBufs,
			m_ResourcesBufs = m_ResourcesBufs,
			m_OwnedVehicleBufs = m_OwnedVehicleBufs,
			m_LayoutElementBufs = m_LayoutElementBufs,
			m_OutsideConnections = m_OutsideConnections,
			m_CityData = m_CityData,
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_City = m_CitySystem.City,
			m_SetupData = setupData
		}, m_GoodsDeliveryFacilityQuery, inputDeps);
	}
}
