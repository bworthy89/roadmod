using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
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
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DeliveryTruckAISystem : GameSystemBase
{
	public struct DeliveredStack
	{
		public Entity vehicle;

		public Entity target;

		public Entity location;

		public Resource resource;

		public int amount;

		public Entity costPayer;

		public float distance;

		public bool storageTransfer;

		public bool moneyRefund;

		public bool buildingUpkeep;
	}

	public struct RemoveGuestVehicle
	{
		public Entity m_Vehicle;

		public Entity m_Target;
	}

	[CompilerGenerated]
	public class Actions : GameSystemBase
	{
		private struct TypeHandle
		{
			public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

			public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RW_ComponentLookup;

			public ComponentLookup<BuyingCompany> __Game_Companies_BuyingCompany_RW_ComponentLookup;

			public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RW_ComponentLookup;

			public ComponentLookup<Game.Buildings.CargoTransportStation> __Game_Buildings_CargoTransportStation_RW_ComponentLookup;

			public BufferLookup<StorageTransferRequest> __Game_Companies_StorageTransferRequest_RW_BufferLookup;

			public BufferLookup<ResourceNeeding> __Game_Buildings_ResourceNeeding_RW_BufferLookup;

			public BufferLookup<CurrentTrading> __Game_Companies_CurrentTrading_RW_BufferLookup;

			[ReadOnly]
			public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

			public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RW_BufferLookup;

			[ReadOnly]
			public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
				__Game_Vehicles_DeliveryTruck_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>();
				__Game_Companies_BuyingCompany_RW_ComponentLookup = state.GetComponentLookup<BuyingCompany>();
				__Game_Companies_StorageCompany_RW_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>();
				__Game_Buildings_CargoTransportStation_RW_ComponentLookup = state.GetComponentLookup<Game.Buildings.CargoTransportStation>();
				__Game_Companies_StorageTransferRequest_RW_BufferLookup = state.GetBufferLookup<StorageTransferRequest>();
				__Game_Buildings_ResourceNeeding_RW_BufferLookup = state.GetBufferLookup<ResourceNeeding>();
				__Game_Companies_CurrentTrading_RW_BufferLookup = state.GetBufferLookup<CurrentTrading>();
				__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
				__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
				__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
				__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
				__Game_Vehicles_GuestVehicle_RW_BufferLookup = state.GetBufferLookup<GuestVehicle>();
				__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			}
		}

		private ResourceSystem m_ResourceSystem;

		private CityStatisticsSystem m_CityStatisticsSystem;

		private EntityQuery m_EconomyParameterQuery;

		public JobHandle m_Dependency;

		public NativeQueue<DeliveredStack> m_DeliveredQueue;

		public NativeQueue<RemoveGuestVehicle> m_RemoveGuestVehicleQueue;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
			m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
			m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
			JobHandle job = JobHandle.CombineDependencies(base.Dependency, m_Dependency);
			JobHandle deps;
			JobHandle jobHandle = IJobExtensions.Schedule(new DeliverJob
			{
				m_DeliveredQueue = m_DeliveredQueue,
				m_RemoveGuestVehicleQueue = m_RemoveGuestVehicleQueue,
				m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
				m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RW_ComponentLookup, ref base.CheckedStateRef),
				m_BuyingCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_BuyingCompany_RW_ComponentLookup, ref base.CheckedStateRef),
				m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CargoTransportStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CargoTransportStation_RW_ComponentLookup, ref base.CheckedStateRef),
				m_StorageTransferRequests = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_StorageTransferRequest_RW_BufferLookup, ref base.CheckedStateRef),
				m_ResourceNeedingBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ResourceNeeding_RW_BufferLookup, ref base.CheckedStateRef),
				m_CurrentTradingBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_CurrentTrading_RW_BufferLookup, ref base.CheckedStateRef),
				m_Companies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Controllers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GuestVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RW_BufferLookup, ref base.CheckedStateRef),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
				m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>()
			}, JobHandle.CombineDependencies(job, deps));
			m_DeliveredQueue.Dispose(jobHandle);
			m_RemoveGuestVehicleQueue.Dispose(jobHandle);
			m_ResourceSystem.AddPrefabsReader(jobHandle);
			m_CityStatisticsSystem.AddWriter(jobHandle);
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
		public Actions()
		{
		}
	}

	[BurstCompile]
	private struct DeliverJob : IJob
	{
		public NativeQueue<DeliveredStack> m_DeliveredQueue;

		public NativeQueue<RemoveGuestVehicle> m_RemoveGuestVehicleQueue;

		public BufferLookup<Game.Economy.Resources> m_Resources;

		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		public ComponentLookup<BuyingCompany> m_BuyingCompanies;

		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		public ComponentLookup<Game.Buildings.CargoTransportStation> m_CargoTransportStationData;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_Companies;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public ComponentLookup<Controller> m_Controllers;

		public BufferLookup<StorageTransferRequest> m_StorageTransferRequests;

		public BufferLookup<GuestVehicle> m_GuestVehicles;

		public BufferLookup<ResourceNeeding> m_ResourceNeedingBufs;

		public BufferLookup<CurrentTrading> m_CurrentTradingBufs;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		public EconomyParameterData m_EconomyParameters;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		private Entity FindCompany(DynamicBuffer<Renter> renters)
		{
			for (int i = 0; i < renters.Length; i++)
			{
				if (m_Companies.HasComponent(renters[i].m_Renter))
				{
					return renters[i].m_Renter;
				}
			}
			return Entity.Null;
		}

		private void AddWork(Entity target, int workAmount)
		{
			if (workAmount <= 0)
			{
				return;
			}
			Owner componentData;
			while (m_Owners.TryGetComponent(target, out componentData))
			{
				target = componentData.m_Owner;
				if (m_CargoTransportStationData.TryGetComponent(target, out var componentData2))
				{
					componentData2.m_WorkAmount += workAmount;
					m_CargoTransportStationData[target] = componentData2;
					break;
				}
			}
		}

		public void Execute()
		{
			DeliveredStack item;
			while (m_DeliveredQueue.TryDequeue(out item))
			{
				if (m_ResourcePrefabs[item.resource] == Entity.Null)
				{
					continue;
				}
				if (item.buildingUpkeep && m_ResourceNeedingBufs.HasBuffer(item.target))
				{
					DynamicBuffer<ResourceNeeding> dynamicBuffer = m_ResourceNeedingBufs[item.target];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						ResourceNeeding value = dynamicBuffer[i];
						if (item.resource == value.m_Resource && item.amount == value.m_Amount)
						{
							value.m_Flags = ResourceNeedingFlags.Delivered;
							dynamicBuffer[i] = value;
							break;
						}
					}
					continue;
				}
				int num = Mathf.RoundToInt((float)item.amount * EconomyUtils.GetIndustrialPrice(item.resource, m_ResourcePrefabs, ref m_ResourceDatas));
				float weight = EconomyUtils.GetWeight(item.resource, m_ResourcePrefabs, ref m_ResourceDatas);
				int num2 = Mathf.RoundToInt(EconomyUtils.GetTransportCost(item.distance, item.resource, item.amount, weight));
				if (!m_Resources.HasBuffer(item.target) && m_Renters.HasBuffer(item.target))
				{
					item.target = FindCompany(m_Renters[item.target]);
				}
				if (!m_Resources.HasBuffer(item.costPayer) && m_Renters.HasBuffer(item.costPayer))
				{
					item.costPayer = FindCompany(m_Renters[item.costPayer]);
				}
				if (!m_Resources.HasBuffer(item.target))
				{
					if (item.moneyRefund && m_Resources.HasBuffer(item.costPayer) && !m_StorageCompanies.HasComponent(item.costPayer))
					{
						EconomyUtils.AddResources(Resource.Money, num, m_Resources[item.costPayer]);
					}
					continue;
				}
				DynamicBuffer<Game.Economy.Resources> resources = m_Resources[item.target];
				if (item.amount < 0)
				{
					int num3 = math.min(-item.amount, EconomyUtils.GetResources(item.resource, resources));
					Game.Vehicles.DeliveryTruck value2 = m_DeliveryTrucks[item.vehicle];
					value2.m_Amount += num3;
					m_DeliveryTrucks[item.vehicle] = value2;
					EconomyUtils.AddResources(item.resource, -num3, resources);
					if (m_CurrentTradingBufs.HasBuffer(item.costPayer))
					{
						DynamicBuffer<CurrentTrading> dynamicBuffer2 = m_CurrentTradingBufs[item.costPayer];
						for (int j = 0; j < dynamicBuffer2.Length; j++)
						{
							CurrentTrading currentTrading = dynamicBuffer2[j];
							if (currentTrading.m_TradingResource == item.resource && math.abs(currentTrading.m_TradingResourceAmount) == item.amount)
							{
								dynamicBuffer2.RemoveAt(j);
								break;
							}
						}
					}
				}
				else
				{
					if (!item.moneyRefund)
					{
						EconomyUtils.AddResources(item.resource, item.amount, resources);
						m_StatisticsEventQueue.Enqueue(new StatisticsEvent
						{
							m_Statistic = StatisticType.CargoCountTruck,
							m_Change = item.amount
						});
						if (m_CurrentTradingBufs.HasBuffer(item.costPayer))
						{
							DynamicBuffer<CurrentTrading> dynamicBuffer3 = m_CurrentTradingBufs[item.costPayer];
							for (int k = 0; k < dynamicBuffer3.Length; k++)
							{
								CurrentTrading currentTrading2 = dynamicBuffer3[k];
								if (currentTrading2.m_TradingResource == item.resource && math.abs(currentTrading2.m_TradingResourceAmount) == item.amount)
								{
									dynamicBuffer3.RemoveAt(k);
									break;
								}
							}
						}
					}
					if (m_Resources.HasBuffer(item.costPayer))
					{
						if (item.moneyRefund)
						{
							if (!m_StorageCompanies.HasComponent(item.costPayer))
							{
								EconomyUtils.AddResources(Resource.Money, num, m_Resources[item.costPayer]);
							}
							if (!m_StorageCompanies.HasComponent(item.target))
							{
								EconomyUtils.AddResources(Resource.Money, -num, m_Resources[item.target]);
							}
						}
						else if (item.storageTransfer)
						{
							if (!m_StorageCompanies.HasComponent(item.costPayer))
							{
								EconomyUtils.AddResources(Resource.Money, -num2, m_Resources[item.costPayer]);
							}
							if (m_BuyingCompanies.HasComponent(item.costPayer))
							{
								BuyingCompany value3 = m_BuyingCompanies[item.costPayer];
								if (value3.m_MeanInputTripLength > 0f)
								{
									value3.m_MeanInputTripLength = math.lerp(value3.m_MeanInputTripLength, item.distance, 0.5f);
								}
								else
								{
									value3.m_MeanInputTripLength = item.distance;
								}
								m_BuyingCompanies[item.costPayer] = value3;
							}
						}
						else
						{
							if (!m_StorageCompanies.HasComponent(item.costPayer))
							{
								EconomyUtils.AddResources(Resource.Money, num, m_Resources[item.costPayer]);
							}
							if (!m_StorageCompanies.HasComponent(item.target))
							{
								EconomyUtils.AddResources(Resource.Money, -num, m_Resources[item.target]);
							}
						}
					}
					if (item.amount > 0 && m_StorageCompanies.HasComponent(item.target) && m_StorageTransferRequests.HasBuffer(item.target) && (m_Owners.HasComponent(item.vehicle) || (m_Controllers.HasComponent(item.vehicle) && m_Owners.HasComponent(m_Controllers[item.vehicle].m_Controller))))
					{
						Entity entity = (m_Controllers.HasComponent(item.vehicle) ? m_Controllers[item.vehicle].m_Controller : item.vehicle);
						Entity owner = m_Owners[entity].m_Owner;
						DynamicBuffer<StorageTransferRequest> dynamicBuffer4 = m_StorageTransferRequests[item.target];
						for (int l = 0; l < dynamicBuffer4.Length; l++)
						{
							StorageTransferRequest value4 = dynamicBuffer4[l];
							if ((value4.m_Flags & StorageTransferFlags.Incoming) != 0 && value4.m_Target == owner && value4.m_Resource == item.resource)
							{
								if (value4.m_Amount > item.amount)
								{
									value4.m_Amount -= item.amount;
									dynamicBuffer4[l] = value4;
									break;
								}
								item.amount -= value4.m_Amount;
								dynamicBuffer4.RemoveAtSwapBack(l);
								l--;
							}
						}
					}
				}
				AddWork(item.location, math.abs(item.amount));
			}
			RemoveGuestVehicle item2;
			while (m_RemoveGuestVehicleQueue.TryDequeue(out item2))
			{
				if (m_GuestVehicles.HasBuffer(item2.m_Target))
				{
					CollectionUtils.RemoveValue(m_GuestVehicles[item2.m_Target], new GuestVehicle(item2.m_Vehicle));
				}
			}
		}
	}

	[BurstCompile]
	private struct DeliveryTruckTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		public ComponentTypeHandle<Car> m_CarType;

		public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<CarNavigationLane> m_CarNavigationLaneType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyData;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryVehicle> m_GoodsDeliveryVehicles;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryRequest> m_GoodsDeliveryRequests;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformations;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<ResourceNeeding> m_ResourceNeedingBufs;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTruckData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ReturnLoad> m_ReturnLoadData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<DeliveredStack>.ParallelWriter m_DeliveredQueue;

		public NativeQueue<RemoveGuestVehicle>.ParallelWriter m_RemoveGuestVehicleQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Car> nativeArray4 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<CarCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Target> nativeArray6 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray7 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<PathInformation> nativeArray8 = chunk.GetNativeArray(ref m_PathInformationType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
			BufferAccessor<CarNavigationLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray3[i];
				PathInformation pathInformation = nativeArray8[i];
				ref Car car = ref nativeArray4.ElementAt(i);
				ref CarCurrentLane reference = ref nativeArray5.ElementAt(i);
				ref PathOwner pathOwner = ref nativeArray7.ElementAt(i);
				ref Target target = ref nativeArray6.ElementAt(i);
				ref Game.Vehicles.DeliveryTruck valueRW = ref m_DeliveryTruckData.GetRefRW(entity).ValueRW;
				CollectionUtils.TryGet(nativeArray2, i, out var value);
				CollectionUtils.TryGet(bufferAccessor, i, out var value2);
				CollectionUtils.TryGet(bufferAccessor2, i, out var value3);
				CollectionUtils.TryGet(bufferAccessor3, i, out var value4);
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, reference, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, value, prefabRef, pathInformation, value2, value4, value3, ref valueRW, ref car, ref reference, ref pathOwner, ref target);
			}
		}

		private void CancelTransaction(int jobIndex, Entity vehicleEntity, ref Game.Vehicles.DeliveryTruck deliveryTruck, DynamicBuffer<LayoutElement> layout, PathInformation pathInformation, Owner owner)
		{
			if ((deliveryTruck.m_State & DeliveryTruckFlags.TransactionCancelled) != 0)
			{
				return;
			}
			if (layout.IsCreated && layout.Length != 0)
			{
				for (int i = 0; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					if (!(vehicle != vehicleEntity) || !m_DeliveryTruckData.HasComponent(vehicle))
					{
						continue;
					}
					Game.Vehicles.DeliveryTruck deliveryTruck2 = m_DeliveryTruckData[vehicle];
					if ((deliveryTruck2.m_State & DeliveryTruckFlags.Loaded) != 0 && deliveryTruck2.m_Amount > 0)
					{
						if ((deliveryTruck.m_State & DeliveryTruckFlags.Returning) != 0)
						{
							DeliveredStack value = new DeliveredStack
							{
								vehicle = vehicle,
								target = owner.m_Owner,
								resource = deliveryTruck2.m_Resource,
								amount = deliveryTruck2.m_Amount,
								costPayer = owner.m_Owner,
								distance = pathInformation.m_Distance
							};
							m_DeliveredQueue.Enqueue(value);
							if (value.amount != 0)
							{
								TargetQuantityUpdated(jobIndex, value.target);
							}
							continue;
						}
						DeliveredStack value2 = new DeliveredStack
						{
							vehicle = vehicle,
							target = pathInformation.m_Origin,
							resource = deliveryTruck2.m_Resource,
							amount = deliveryTruck2.m_Amount,
							costPayer = pathInformation.m_Destination,
							distance = 0f,
							storageTransfer = ((deliveryTruck.m_State & DeliveryTruckFlags.StorageTransfer) != 0)
						};
						m_DeliveredQueue.Enqueue(value2);
						if (value2.amount != 0)
						{
							TargetQuantityUpdated(jobIndex, value2.target);
						}
					}
					else if ((deliveryTruck2.m_State & DeliveryTruckFlags.Loaded) == 0 && (deliveryTruck.m_State & DeliveryTruckFlags.Returning) == 0)
					{
						DeliveredStack value3 = new DeliveredStack
						{
							vehicle = vehicle,
							target = pathInformation.m_Destination,
							resource = deliveryTruck2.m_Resource,
							amount = deliveryTruck2.m_Amount,
							distance = 0f,
							costPayer = owner.m_Owner,
							moneyRefund = true
						};
						m_DeliveredQueue.Enqueue(value3);
						if (value3.amount < 0)
						{
							TargetQuantityUpdated(jobIndex, value3.target);
						}
					}
				}
			}
			else if ((deliveryTruck.m_State & DeliveryTruckFlags.Loaded) != 0 && deliveryTruck.m_Amount > 0)
			{
				if ((deliveryTruck.m_State & DeliveryTruckFlags.Returning) != 0)
				{
					DeliveredStack value4 = new DeliveredStack
					{
						vehicle = vehicleEntity,
						target = owner.m_Owner,
						resource = deliveryTruck.m_Resource,
						amount = deliveryTruck.m_Amount,
						costPayer = owner.m_Owner,
						distance = pathInformation.m_Distance
					};
					m_DeliveredQueue.Enqueue(value4);
					if (value4.amount != 0)
					{
						TargetQuantityUpdated(jobIndex, value4.target);
					}
				}
				else
				{
					DeliveredStack value5 = new DeliveredStack
					{
						vehicle = vehicleEntity,
						target = pathInformation.m_Origin,
						resource = deliveryTruck.m_Resource,
						amount = deliveryTruck.m_Amount,
						costPayer = pathInformation.m_Destination,
						storageTransfer = ((deliveryTruck.m_State & DeliveryTruckFlags.StorageTransfer) != 0)
					};
					m_DeliveredQueue.Enqueue(value5);
					if (value5.amount != 0)
					{
						TargetQuantityUpdated(jobIndex, value5.target);
					}
				}
			}
			else if ((deliveryTruck.m_State & DeliveryTruckFlags.Loaded) == 0 && (deliveryTruck.m_State & DeliveryTruckFlags.Returning) == 0)
			{
				DeliveredStack value6 = new DeliveredStack
				{
					vehicle = vehicleEntity,
					target = pathInformation.m_Destination,
					resource = deliveryTruck.m_Resource,
					amount = deliveryTruck.m_Amount,
					distance = 0f,
					costPayer = owner.m_Owner,
					moneyRefund = true
				};
				m_DeliveredQueue.Enqueue(value6);
				if (value6.amount < 0)
				{
					TargetQuantityUpdated(jobIndex, value6.target);
				}
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<LayoutElement> layout, DynamicBuffer<ServiceDispatch> serviceDispatchBuf, DynamicBuffer<CarNavigationLane> carNavigationLaneBuf, ref Game.Vehicles.DeliveryTruck deliveryTruck, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
				PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
				if (layout.IsCreated && layout.Length >= 2)
				{
					car.m_Flags |= CarFlags.CannotReverse;
				}
				else
				{
					car.m_Flags &= ~CarFlags.CannotReverse;
				}
				if ((deliveryTruck.m_State & DeliveryTruckFlags.UpkeepDelivery) != 0 && layout.IsCreated && layout.Length >= 2)
				{
					car.m_Flags |= CarFlags.StayOnRoad;
					car.m_Flags &= ~CarFlags.CannotReverse;
				}
				if ((deliveryTruck.m_State & DeliveryTruckFlags.UpdateOwnerQuantity) != 0)
				{
					deliveryTruck.m_State &= ~DeliveryTruckFlags.UpdateOwnerQuantity;
					TargetQuantityUpdated(jobIndex, owner.m_Owner);
				}
			}
			if (!m_PrefabRefData.HasComponent(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if ((deliveryTruck.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
				{
					m_RemoveGuestVehicleQueue.Enqueue(new RemoveGuestVehicle
					{
						m_Vehicle = vehicleEntity,
						m_Target = pathInformation.m_Destination
					});
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);
					return;
				}
				if (VehicleUtils.IsStuck(pathOwner) || (deliveryTruck.m_State & DeliveryTruckFlags.Returning) != 0)
				{
					if (VehicleUtils.PathfindFailed(pathOwner) || VehicleUtils.IsStuck(pathOwner))
					{
						CancelTransaction(jobIndex, vehicleEntity, ref deliveryTruck, layout, pathInformation, owner);
						deliveryTruck.m_State |= DeliveryTruckFlags.TransactionCancelled;
					}
					m_RemoveGuestVehicleQueue.Enqueue(new RemoveGuestVehicle
					{
						m_Vehicle = vehicleEntity,
						m_Target = pathInformation.m_Destination
					});
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);
					return;
				}
				if (VehicleUtils.PathfindFailed(pathOwner) || VehicleUtils.IsStuck(pathOwner))
				{
					CancelTransaction(jobIndex, vehicleEntity, ref deliveryTruck, layout, pathInformation, owner);
					deliveryTruck.m_State |= DeliveryTruckFlags.TransactionCancelled;
				}
				deliveryTruck.m_State |= DeliveryTruckFlags.Returning;
				m_RemoveGuestVehicleQueue.Enqueue(new RemoveGuestVehicle
				{
					m_Vehicle = vehicleEntity,
					m_Target = pathInformation.m_Destination
				});
				VehicleUtils.SetTarget(ref pathOwner, ref target, owner.m_Owner);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if ((deliveryTruck.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
				{
					m_RemoveGuestVehicleQueue.Enqueue(new RemoveGuestVehicle
					{
						m_Vehicle = vehicleEntity,
						m_Target = pathInformation.m_Destination
					});
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);
					return;
				}
				bool flag = false;
				if ((deliveryTruck.m_State & DeliveryTruckFlags.UpkeepDelivery) != 0)
				{
					flag = DeliverUpkeep(jobIndex, vehicleEntity, owner.m_Owner, pathInformation, layout, serviceDispatchBuf, carNavigationLaneBuf, ref deliveryTruck, ref car, ref currentLane, ref pathOwner, ref target);
				}
				else
				{
					DeliverCargo(jobIndex, vehicleEntity, owner.m_Owner, pathInformation, layout, ref deliveryTruck, ref currentLane);
				}
				if ((deliveryTruck.m_State & DeliveryTruckFlags.Returning) != 0 || !m_PrefabRefData.HasComponent(owner.m_Owner))
				{
					m_RemoveGuestVehicleQueue.Enqueue(new RemoveGuestVehicle
					{
						m_Vehicle = vehicleEntity,
						m_Target = pathInformation.m_Destination
					});
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, vehicleEntity, layout);
					return;
				}
				if (!flag)
				{
					deliveryTruck.m_State |= DeliveryTruckFlags.Returning;
					m_RemoveGuestVehicleQueue.Enqueue(new RemoveGuestVehicle
					{
						m_Vehicle = vehicleEntity,
						m_Target = pathInformation.m_Destination
					});
					VehicleUtils.SetTarget(ref pathOwner, ref target, owner.m_Owner);
				}
			}
			if ((deliveryTruck.m_State & DeliveryTruckFlags.TransactionCancelled) == 0 && m_GoodsDeliveryVehicles.TryGetComponent(vehicleEntity, out var componentData) && (deliveryTruck.m_State & DeliveryTruckFlags.Returning) != 0)
			{
				SelectNextDispatch(jobIndex, vehicleEntity, carNavigationLaneBuf, serviceDispatchBuf, ref deliveryTruck, ref componentData, ref car, ref currentLane, ref pathOwner, ref target);
			}
			FindPathIfNeeded(vehicleEntity, prefabRef, ref currentLane, ref pathOwner, ref target);
		}

		private bool DeliverUpkeep(int jobIndex, Entity vehicleEntity, Entity truckOwner, PathInformation pathInformation, DynamicBuffer<LayoutElement> layoutElementBuf, DynamicBuffer<ServiceDispatch> serviceDispatchBuf, DynamicBuffer<CarNavigationLane> carNavigationLaneBuf, ref Game.Vehicles.DeliveryTruck deliveryTruck, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (!m_GoodsDeliveryVehicles.TryGetComponent(vehicleEntity, out var componentData))
			{
				return false;
			}
			bool flag = false;
			Resource resource = deliveryTruck.m_Resource;
			if (resource == Resource.NoResource && layoutElementBuf.IsCreated && layoutElementBuf.Length != 0)
			{
				for (int i = 0; i < layoutElementBuf.Length; i++)
				{
					Entity vehicle = layoutElementBuf[i].m_Vehicle;
					if (vehicle != vehicleEntity && m_DeliveryTruckData.HasComponent(vehicle) && m_DeliveryTruckData[vehicle].m_Resource != Resource.NoResource)
					{
						resource = m_DeliveryTruckData[vehicle].m_Resource;
						break;
					}
				}
			}
			int num = 0;
			if (m_ResourceNeedingBufs.HasBuffer(target.m_Target))
			{
				DynamicBuffer<ResourceNeeding> dynamicBuffer = m_ResourceNeedingBufs[target.m_Target];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					ResourceNeeding resourceNeeding = dynamicBuffer[j];
					if (resource == resourceNeeding.m_Resource && resourceNeeding.m_Flags < ResourceNeedingFlags.Delivered)
					{
						num = resourceNeeding.m_Amount;
						break;
					}
				}
			}
			if (layoutElementBuf.IsCreated && layoutElementBuf.Length != 0)
			{
				for (int k = 0; k < layoutElementBuf.Length; k++)
				{
					Entity vehicle2 = layoutElementBuf[k].m_Vehicle;
					if (!(vehicle2 != vehicleEntity) || !m_DeliveryTruckData.HasComponent(vehicle2))
					{
						continue;
					}
					Game.Vehicles.DeliveryTruck value = m_DeliveryTruckData[vehicle2];
					if ((value.m_State & DeliveryTruckFlags.Loaded) != 0 && value.m_Amount > 0)
					{
						DeliveredStack value2 = new DeliveredStack
						{
							vehicle = vehicleEntity,
							target = target.m_Target,
							location = currentLane.m_Lane,
							resource = resource,
							amount = num,
							costPayer = target.m_Target,
							distance = pathInformation.m_Distance,
							buildingUpkeep = true
						};
						m_DeliveredQueue.Enqueue(value2);
						value.m_Amount -= num;
						flag |= value2.amount != 0;
						QuantityUpdated(jobIndex, vehicle2, resourceChanged: true);
						if (value.m_Amount <= 0)
						{
							value.m_State &= ~DeliveryTruckFlags.Loaded;
						}
						m_DeliveryTruckData[vehicle2] = value;
					}
				}
			}
			else if ((deliveryTruck.m_State & DeliveryTruckFlags.Loaded) != 0 && deliveryTruck.m_Amount > 0)
			{
				DeliveredStack value3 = new DeliveredStack
				{
					vehicle = vehicleEntity,
					target = target.m_Target,
					location = currentLane.m_Lane,
					resource = resource,
					amount = num,
					costPayer = target.m_Target,
					distance = pathInformation.m_Distance,
					buildingUpkeep = true
				};
				m_DeliveredQueue.Enqueue(value3);
				deliveryTruck.m_Amount -= num;
				flag |= value3.amount != 0;
				if (deliveryTruck.m_Amount <= 0)
				{
					deliveryTruck.m_State &= ~DeliveryTruckFlags.Loaded;
				}
			}
			QuantityUpdated(jobIndex, vehicleEntity, resourceChanged: true);
			if (flag)
			{
				TargetQuantityUpdated(jobIndex, target.m_Target);
			}
			return SelectNextDispatch(jobIndex, vehicleEntity, carNavigationLaneBuf, serviceDispatchBuf, ref deliveryTruck, ref componentData, ref car, ref currentLane, ref pathOwner, ref target);
		}

		private void DeliverCargo(int jobIndex, Entity truck, Entity truckOwner, PathInformation pathInformation, DynamicBuffer<LayoutElement> layout, ref Game.Vehicles.DeliveryTruck truckDelivery, ref CarCurrentLane currentLane)
		{
			bool resourceChanged = false;
			bool flag = false;
			if (layout.IsCreated && layout.Length != 0)
			{
				for (int i = 0; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					if (!(vehicle != truck) || !m_DeliveryTruckData.HasComponent(vehicle))
					{
						continue;
					}
					Game.Vehicles.DeliveryTruck value = m_DeliveryTruckData[vehicle];
					if ((value.m_State & DeliveryTruckFlags.Loaded) != 0 && value.m_Amount > 0)
					{
						DeliveredStack value2 = new DeliveredStack
						{
							vehicle = vehicle,
							target = pathInformation.m_Destination,
							location = currentLane.m_Lane,
							resource = value.m_Resource,
							amount = value.m_Amount,
							costPayer = truckOwner,
							distance = pathInformation.m_Distance
						};
						m_DeliveredQueue.Enqueue(value2);
						flag |= value2.amount != 0;
					}
					value.m_State ^= DeliveryTruckFlags.Loaded;
					if ((value.m_State & DeliveryTruckFlags.Loaded) == 0 && (truckDelivery.m_State & DeliveryTruckFlags.Returning) == 0 && m_ReturnLoadData.HasComponent(vehicle))
					{
						ReturnLoad returnLoad = m_ReturnLoadData[vehicle];
						if (returnLoad.m_Amount > 0)
						{
							DeliveredStack value3 = new DeliveredStack
							{
								vehicle = vehicle,
								target = pathInformation.m_Destination,
								location = currentLane.m_Lane,
								resource = returnLoad.m_Resource,
								amount = -returnLoad.m_Amount,
								costPayer = truckOwner,
								distance = pathInformation.m_Distance
							};
							m_DeliveredQueue.Enqueue(value3);
							flag |= value3.amount != 0;
							value.m_State |= DeliveryTruckFlags.Loaded;
							value.m_Resource = returnLoad.m_Resource;
							value.m_Amount = 0;
							resourceChanged = true;
						}
						m_ReturnLoadData[vehicle] = default(ReturnLoad);
					}
					m_DeliveryTruckData[vehicle] = value;
					QuantityUpdated(jobIndex, vehicle, resourceChanged);
				}
			}
			if ((truckDelivery.m_State & DeliveryTruckFlags.Loaded) != 0 && truckDelivery.m_Amount > 0)
			{
				DeliveredStack value4 = new DeliveredStack
				{
					vehicle = truck,
					target = pathInformation.m_Destination,
					location = currentLane.m_Lane,
					resource = truckDelivery.m_Resource,
					amount = truckDelivery.m_Amount,
					costPayer = truckOwner,
					distance = pathInformation.m_Distance,
					buildingUpkeep = false
				};
				m_DeliveredQueue.Enqueue(value4);
				flag |= value4.amount != 0;
			}
			if ((truckDelivery.m_State & DeliveryTruckFlags.UpdateSellerQuantity) != 0)
			{
				truckDelivery.m_State &= ~DeliveryTruckFlags.UpdateSellerQuantity;
				int amount = truckDelivery.m_Amount;
				truckDelivery.m_Amount = 0;
				DeliveredStack value5 = new DeliveredStack
				{
					vehicle = truck,
					target = pathInformation.m_Destination,
					location = currentLane.m_Lane,
					resource = truckDelivery.m_Resource,
					amount = -amount,
					costPayer = truckOwner,
					distance = pathInformation.m_Distance
				};
				m_DeliveredQueue.Enqueue(value5);
				flag |= value5.amount != 0;
				if (layout.IsCreated && layout.Length != 0)
				{
					for (int j = 0; j < layout.Length; j++)
					{
						Entity vehicle2 = layout[j].m_Vehicle;
						if (vehicle2 != truck && m_DeliveryTruckData.HasComponent(vehicle2))
						{
							Game.Vehicles.DeliveryTruck value6 = m_DeliveryTruckData[vehicle2];
							int amount2 = value6.m_Amount;
							value6.m_Amount = 0;
							m_DeliveryTruckData[vehicle2] = value6;
							DeliveredStack value7 = new DeliveredStack
							{
								vehicle = vehicle2,
								target = pathInformation.m_Destination,
								location = currentLane.m_Lane,
								resource = value6.m_Resource,
								amount = -amount2,
								costPayer = truckOwner,
								distance = pathInformation.m_Distance
							};
							m_DeliveredQueue.Enqueue(value7);
							flag |= value7.amount != 0;
						}
					}
				}
			}
			truckDelivery.m_State ^= DeliveryTruckFlags.Loaded;
			resourceChanged = false;
			if ((truckDelivery.m_State & (DeliveryTruckFlags.Returning | DeliveryTruckFlags.Loaded)) == 0 && m_ReturnLoadData.HasComponent(truck))
			{
				ReturnLoad returnLoad2 = m_ReturnLoadData[truck];
				if (returnLoad2.m_Amount > 0)
				{
					DeliveredStack value8 = new DeliveredStack
					{
						vehicle = truck,
						target = pathInformation.m_Destination,
						location = currentLane.m_Lane,
						resource = returnLoad2.m_Resource,
						amount = -returnLoad2.m_Amount,
						costPayer = truckOwner,
						distance = pathInformation.m_Distance
					};
					m_DeliveredQueue.Enqueue(value8);
					flag |= value8.amount != 0;
					truckDelivery.m_State |= DeliveryTruckFlags.Loaded;
					truckDelivery.m_Resource = returnLoad2.m_Resource;
					truckDelivery.m_Amount = 0;
					resourceChanged = true;
				}
				m_ReturnLoadData[truck] = default(ReturnLoad);
			}
			if (resourceChanged)
			{
				truckDelivery.m_State |= DeliveryTruckFlags.Buying;
			}
			QuantityUpdated(jobIndex, truck, resourceChanged);
			if (flag)
			{
				TargetQuantityUpdated(jobIndex, pathInformation.m_Destination);
			}
		}

		private void QuantityUpdated(int jobIndex, Entity vehicleEntity, bool resourceChanged)
		{
			if (!m_SubObjects.HasBuffer(vehicleEntity))
			{
				return;
			}
			if (resourceChanged)
			{
				m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Updated));
				return;
			}
			DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = m_SubObjects[vehicleEntity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subObject = dynamicBuffer[i].m_SubObject;
				m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
			}
		}

		private void TargetQuantityUpdated(int jobIndex, Entity buildingEntity, bool updateAll = false)
		{
			if (m_PropertyData.TryGetComponent(buildingEntity, out var componentData))
			{
				buildingEntity = componentData.m_Property;
			}
			if (!m_SubObjects.TryGetBuffer(buildingEntity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				bool updateAll2 = false;
				if (updateAll || m_QuantityData.HasComponent(subObject))
				{
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
					updateAll2 = true;
				}
				TargetQuantityUpdated(jobIndex, subObject, updateAll2);
			}
		}

		private void FindPathIfNeeded(Entity vehicleEntity, PrefabRef prefabRef, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (VehicleUtils.RequireNewPath(pathOwner))
			{
				CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
				PathfindParameters parameters = new PathfindParameters
				{
					m_MaxSpeed = carData.m_MaxSpeed,
					m_WalkSpeed = 5.555556f,
					m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
					m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
					m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData)
				};
				SetupQueueTarget origin = new SetupQueueTarget
				{
					m_Type = SetupTargetType.CurrentLocation,
					m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
					m_RoadTypes = RoadTypes.Car,
					m_RandomCost = 30f
				};
				SetupQueueTarget destination = new SetupQueueTarget
				{
					m_Type = SetupTargetType.CurrentLocation,
					m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
					m_RoadTypes = RoadTypes.Car,
					m_Entity = target.m_Target,
					m_RandomCost = 30f
				};
				VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<CarNavigationLane> carNavigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.DeliveryTruck deliveryTruck, ref GoodsDeliveryVehicle goodsDeliveryVehicle, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if ((deliveryTruck.m_State & DeliveryTruckFlags.Returning) == 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
			}
			while (serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				Entity entity = Entity.Null;
				if (m_GoodsDeliveryRequests.TryGetComponent(request, out var componentData))
				{
					entity = componentData.m_ResourceNeeder;
				}
				if (!m_EntityLookup.Exists(entity))
				{
					serviceDispatches.RemoveAt(0);
					continue;
				}
				deliveryTruck.m_State &= ~DeliveryTruckFlags.Returning;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = goodsDeliveryVehicle.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformations[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, carNavigationLanes, dynamicBuffer, appendPath, m_SlaveLaneData, m_OwnerData, m_SubLanes, out var _))
						{
							car.m_Flags |= CarFlags.StayOnRoad;
							goodsDeliveryVehicle.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
							target.m_Target = entity;
							VehicleUtils.ClearEndOfPath(ref currentLane, carNavigationLanes);
							return true;
						}
					}
				}
				VehicleUtils.SetTarget(ref pathOwner, ref target, entity);
				return true;
			}
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
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<Car> __Game_Vehicles_Car_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryRequest> __Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryVehicle> __Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ResourceNeeding> __Game_Buildings_ResourceNeeding_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RW_ComponentLookup;

		public ComponentLookup<ReturnLoad> __Game_Vehicles_ReturnLoad_RW_ComponentLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Car_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Car>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup = state.GetComponentLookup<GoodsDeliveryRequest>(isReadOnly: true);
			__Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentLookup = state.GetComponentLookup<GoodsDeliveryVehicle>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Buildings_ResourceNeeding_RO_BufferLookup = state.GetBufferLookup<ResourceNeeding>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>();
			__Game_Vehicles_ReturnLoad_RW_ComponentLookup = state.GetComponentLookup<ReturnLoad>();
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private Actions m_Actions;

	private EntityQuery m_DeliveryTruckQuery;

	private EntityArchetype m_HandleRequestArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_Actions = base.World.GetOrCreateSystemManaged<Actions>();
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_DeliveryTruckQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Vehicles.DeliveryTruck>(), ComponentType.ReadOnly<CarCurrentLane>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = m_SimulationSystem.frameIndex % 16;
		m_DeliveryTruckQuery.ResetFilter();
		m_DeliveryTruckQuery.SetSharedComponentFilter(new UpdateFrame(index));
		m_Actions.m_DeliveredQueue = new NativeQueue<DeliveredStack>(Allocator.TempJob);
		m_Actions.m_RemoveGuestVehicleQueue = new NativeQueue<RemoveGuestVehicle>(Allocator.TempJob);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new DeliveryTruckTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GoodsDeliveryRequests = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GoodsDeliveryVehicles = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceNeedingBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ResourceNeeding_RO_BufferLookup, ref base.CheckedStateRef),
			m_DeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ReturnLoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ReturnLoad_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_DeliveredQueue = m_Actions.m_DeliveredQueue.AsParallelWriter(),
			m_RemoveGuestVehicleQueue = m_Actions.m_RemoveGuestVehicleQueue.AsParallelWriter()
		}, m_DeliveryTruckQuery, base.Dependency);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_Actions.m_Dependency = jobHandle;
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
	public DeliveryTruckAISystem()
	{
	}
}
