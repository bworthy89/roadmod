using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CityServiceUpkeepSystem : GameSystemBase
{
	[BurstCompile]
	private struct CityServiceUpkeepJob : IJobChunk
	{
		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleBufType;

		public BufferTypeHandle<Game.Economy.Resources> m_ResourcesType;

		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> m_ResourceConsumerType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjects;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> m_ServiceUpkeepDatas;

		[ReadOnly]
		public BufferLookup<UpkeepModifierData> m_UpkeepModifiers;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<ResourceConsumerData> m_ResourceConsumerDatas;

		[ReadOnly]
		public ComponentLookup<ServiceUsage> m_ServiceUsages;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_Limits;

		[ReadOnly]
		public BufferLookup<ServiceBudgetData> m_ServiceBudgetDatas;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		public ComponentLookup<PlayerMoney> m_PlayerMoney;

		public NativeArray<int> m_UpkeepAccumulator;

		public uint m_UpdateFrameIndex;

		public Entity m_City;

		public Entity m_BudgetDataEntity;

		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public EntityCommandBuffer m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			DynamicBuffer<ServiceBudgetData> serviceBudgets = m_ServiceBudgetDatas[m_BudgetDataEntity];
			NativeList<ServiceUpkeepData> nativeList = new NativeList<ServiceUpkeepData>(4, Allocator.Temp);
			NativeList<UpkeepModifierData> nativeList2 = new NativeList<UpkeepModifierData>(4, Allocator.Temp);
			NativeParallelHashMap<Entity, bool> notifications = new NativeParallelHashMap<Entity, bool>(4, Allocator.Temp);
			NativeArray<int> nativeArray = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<OwnedVehicle> bufferAccessor = chunk.GetBufferAccessor(ref m_OwnedVehicleBufType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourcesType);
			NativeArray<Game.Buildings.ResourceConsumer> nativeArray4 = chunk.GetNativeArray(ref m_ResourceConsumerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				nativeList.Clear();
				nativeList2.Clear();
				nativeArray.Fill(0);
				Entity entity = nativeArray2[i];
				Entity prefab = nativeArray3[i].m_Prefab;
				DynamicBuffer<Game.Economy.Resources> resources = bufferAccessor2[i];
				int serviceBudget = GetServiceBudget(prefab, serviceBudgets);
				GetUpkeepWithUsageScale(nativeList, m_ServiceUpkeepDatas, m_InstalledUpgrades, m_Prefabs, m_ServiceUsages, entity, prefab, mainBuildingDisabled: false);
				GetUpkeepModifierData(nativeList2, m_InstalledUpgrades, m_Prefabs, m_UpkeepModifiers, entity);
				Unity.Mathematics.Random random = m_RandomSeed.GetRandom(entity.Index);
				random.NextBool();
				GetStorageTargets(nativeArray, nativeList2, entity, prefab);
				m_Limits.TryGetComponent(prefab, out var componentData);
				if (m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData))
				{
					UpgradeUtils.CombineStats(ref componentData, bufferData, ref m_Prefabs, ref m_Limits);
				}
				bool flag = TickConsumer(serviceBudget, componentData.m_Limit, nativeList, nativeList2, resources, ref random);
				int num = 0;
				foreach (int item in nativeArray)
				{
					num += item;
				}
				if (num > 0)
				{
					notifications.Clear();
					if (nativeArray4.Length != 0)
					{
						ref Game.Buildings.ResourceConsumer reference = ref nativeArray4.ElementAt(i);
						bool wasEmpty = reference.m_ResourceAvailability == 0;
						reference.m_ResourceAvailability = GetResourceAvailability(nativeList, resources, nativeArray);
						bool isEmpty = reference.m_ResourceAvailability == 0;
						UpdateNotification(notifications, prefab, wasEmpty, isEmpty);
					}
					foreach (KeyValue<Entity, bool> item2 in notifications)
					{
						if (item2.Value)
						{
							m_IconCommandBuffer.Add(entity, item2.Key, IconPriority.Problem);
						}
						else
						{
							m_IconCommandBuffer.Remove(entity, item2.Key);
						}
					}
					int num2;
					if (componentData.m_Limit > 0)
					{
						num2 = componentData.m_Limit;
						float num3 = (float)componentData.m_Limit / (float)num;
						for (int j = 0; j < nativeArray.Length; j++)
						{
							nativeArray[j] = Mathf.RoundToInt(num3 * (float)nativeArray[j]);
						}
					}
					else
					{
						num2 = num;
					}
					num2 -= EconomyUtils.GetTotalStorageUsed(resources);
					int min;
					if (bufferAccessor.Length != 0)
					{
						DynamicBuffer<OwnedVehicle> dynamicBuffer = bufferAccessor[i];
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							Entity vehicle = dynamicBuffer[k].m_Vehicle;
							if (!m_DeliveryTrucks.TryGetComponent(vehicle, out var componentData2) || (componentData2.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
							{
								continue;
							}
							if (m_LayoutElements.TryGetBuffer(vehicle, out var bufferData2) && bufferData2.Length != 0)
							{
								for (int l = 0; l < bufferData2.Length; l++)
								{
									if (m_DeliveryTrucks.TryGetComponent(bufferData2[l].m_Vehicle, out var componentData3) && componentData3.m_Resource != Resource.NoResource)
									{
										num2 -= componentData3.m_Amount;
										ref NativeArray<int> reference2 = ref nativeArray;
										min = EconomyUtils.GetResourceIndex(componentData3.m_Resource);
										reference2[min] -= componentData3.m_Amount;
									}
								}
							}
							else if (componentData2.m_Resource != Resource.NoResource)
							{
								num2 -= componentData2.m_Amount;
								ref NativeArray<int> reference2 = ref nativeArray;
								min = EconomyUtils.GetResourceIndex(componentData2.m_Resource);
								reference2[min] -= componentData2.m_Amount;
							}
						}
					}
					for (int m = 0; m < nativeArray.Length; m++)
					{
						int num4 = nativeArray[m];
						if (num4 <= 0)
						{
							continue;
						}
						Resource resource = EconomyUtils.GetResource(m);
						int resources2 = EconomyUtils.GetResources(resource, resources);
						m_DeliveryTruckSelectData.GetCapacityRange(resource, out min, out var max);
						int num5 = num4 - resources2;
						if (num2 > 0 && num5 > 0)
						{
							float num6 = (float)resources2 / (float)num4;
							float num7 = ((num6 < 0.25f) ? 1f : ((num6 >= 0.9f) ? 0f : math.lerp(1f, 0.3f, (num6 - 0.25f) / 0.65f)));
							if (random.NextFloat(1f) < num7)
							{
								int num8 = math.min(num5, num2);
								num2 -= num8;
								ResourceBuyer component = new ResourceBuyer
								{
									m_Payer = entity,
									m_AmountNeeded = math.min(num8, max),
									m_Flags = (SetupTargetFlags.Industrial | SetupTargetFlags.Import),
									m_ResourceNeeded = resource
								};
								m_CommandBuffer.AddComponent(entity, component);
							}
						}
					}
				}
				EconomyUtils.GetResources(Resource.Money, resources);
				_ = 0;
				if (flag)
				{
					QuantityUpdated(entity);
				}
			}
		}

		private void GetStorageTargets(NativeArray<int> storageTargets, NativeList<UpkeepModifierData> upkeepModifiers, Entity entity, Entity prefab)
		{
			if (m_ServiceUpkeepDatas.TryGetBuffer(prefab, out var bufferData))
			{
				foreach (ServiceUpkeepData item in bufferData)
				{
					float x = GetUpkeepModifier(item.m_Upkeep.m_Resource, upkeepModifiers).Transform(item.m_Upkeep.m_Amount);
					if (IsMaterialResource(m_ResourceDatas, m_ResourcePrefabs, item.m_Upkeep))
					{
						storageTargets[EconomyUtils.GetResourceIndex(item.m_Upkeep.m_Resource)] += (int)math.round(x);
					}
				}
			}
			if (!m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData2))
			{
				return;
			}
			foreach (InstalledUpgrade item2 in bufferData2)
			{
				if (BuildingUtils.CheckOption(item2, BuildingOption.Inactive) || !m_Prefabs.TryGetComponent(item2.m_Upgrade, out var componentData) || !m_ServiceUpkeepDatas.TryGetBuffer(componentData.m_Prefab, out var bufferData3))
				{
					continue;
				}
				foreach (ServiceUpkeepData item3 in bufferData3)
				{
					float x2 = GetUpkeepModifier(item3.m_Upkeep.m_Resource, upkeepModifiers).Transform(item3.m_Upkeep.m_Amount);
					if (IsMaterialResource(m_ResourceDatas, m_ResourcePrefabs, item3.m_Upkeep))
					{
						storageTargets[EconomyUtils.GetResourceIndex(item3.m_Upkeep.m_Resource)] += (int)math.round(x2);
					}
				}
			}
		}

		private int GetServiceBudget(Entity prefab, DynamicBuffer<ServiceBudgetData> serviceBudgets)
		{
			if (m_ServiceObjects.TryGetComponent(prefab, out var componentData))
			{
				for (int i = 0; i < serviceBudgets.Length; i++)
				{
					if (serviceBudgets[i].m_Service == componentData.m_Service)
					{
						return serviceBudgets[i].m_Budget;
					}
				}
			}
			return 100;
		}

		private bool TickConsumer(int serviceBudget, int storageLimit, NativeList<ServiceUpkeepData> serviceUpkeepDatas, NativeList<UpkeepModifierData> upkeepModifiers, DynamicBuffer<Game.Economy.Resources> resources, ref Unity.Mathematics.Random random)
		{
			bool flag = false;
			foreach (ServiceUpkeepData item in serviceUpkeepDatas)
			{
				Resource resource = item.m_Upkeep.m_Resource;
				bool flag2 = IsMaterialResource(m_ResourceDatas, m_ResourcePrefabs, item.m_Upkeep);
				if (item.m_Upkeep.m_Amount <= 0)
				{
					continue;
				}
				float num = GetUpkeepModifier(resource, upkeepModifiers).Transform(item.m_Upkeep.m_Amount);
				int num2 = MathUtils.RoundToIntRandom(ref random, num / (float)kUpdatesPerDay);
				if (num2 > 0)
				{
					_ = m_PlayerMoney[m_City];
					if (flag2)
					{
						int num3 = EconomyUtils.AddResources(resource, -num2, resources);
						int num4 = num3 + num2;
						num4 = Mathf.RoundToInt((float)num4 / (float)math.max(1, storageLimit) * 100f);
						num3 = Mathf.RoundToInt((float)num3 / (float)math.max(1, storageLimit) * 100f);
						int4 @int = new int4(0, 33, 50, 66);
						flag |= math.any(num4 > @int != num3 > @int);
						int resourceIndex = EconomyUtils.GetResourceIndex(resource);
						m_UpkeepAccumulator[resourceIndex] += num3;
					}
				}
			}
			return flag;
		}

		private void QuantityUpdated(Entity buildingEntity, bool updateAll = false)
		{
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
					m_CommandBuffer.AddComponent(subObject, default(BatchesUpdated));
					updateAll2 = true;
				}
				QuantityUpdated(subObject, updateAll2);
			}
		}

		private void UpdateNotification(NativeParallelHashMap<Entity, bool> notifications, Entity prefab, bool wasEmpty, bool isEmpty)
		{
			if (m_ResourceConsumerDatas.TryGetComponent(prefab, out var componentData) && componentData.m_NoResourceNotificationPrefab != Entity.Null && wasEmpty != isEmpty && (!isEmpty || !notifications.ContainsKey(componentData.m_NoResourceNotificationPrefab)))
			{
				notifications[componentData.m_NoResourceNotificationPrefab] = isEmpty;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<UpkeepModifierData> __Game_Prefabs_UpkeepModifierData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceConsumerData> __Game_Prefabs_ResourceConsumerData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUsage> __Game_Buildings_ServiceUsage_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceBudgetData> __Game_Simulation_ServiceBudgetData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>();
			__Game_Buildings_ResourceConsumer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ResourceConsumer>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
			__Game_Prefabs_UpkeepModifierData_RO_BufferLookup = state.GetBufferLookup<UpkeepModifierData>(isReadOnly: true);
			__Game_Prefabs_ResourceConsumerData_RO_ComponentLookup = state.GetComponentLookup<ResourceConsumerData>(isReadOnly: true);
			__Game_Buildings_ServiceUsage_RO_ComponentLookup = state.GetComponentLookup<ServiceUsage>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Simulation_ServiceBudgetData_RO_BufferLookup = state.GetBufferLookup<ServiceBudgetData>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_City_PlayerMoney_RW_ComponentLookup = state.GetComponentLookup<PlayerMoney>();
		}
	}

	private static readonly int kUpdatesPerDay = 64;

	private CitySystem m_CitySystem;

	private SimulationSystem m_SimulationSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ResourceSystem m_ResourceSystem;

	private IconCommandSystem m_IconCommandSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private EntityQuery m_UpkeepGroup;

	private EntityQuery m_BudgetDataQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_UpkeepGroup = GetEntityQuery(ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.ReadWrite<Game.Economy.Resources>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_BudgetDataQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceBudgetData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle deps;
		CityServiceUpkeepJob jobData = new CityServiceUpkeepJob
		{
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResourceConsumer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceObjects = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpkeepDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
			m_UpkeepModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UpkeepModifierData_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceConsumerDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceConsumerData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUsages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUsage_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Limits = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceBudgetDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceBudgetData_RO_BufferLookup, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_PlayerMoney = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_PlayerMoney_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpkeepAccumulator = m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.ServiceUpkeep, out deps),
			m_UpdateFrameIndex = updateFrame,
			m_City = m_CitySystem.City,
			m_BudgetDataEntity = m_BudgetDataQuery.GetSingletonEntity(),
			m_RandomSeed = RandomSeed.Next(),
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_UpkeepGroup, JobHandle.CombineDependencies(base.Dependency, deps));
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_CityProductionStatisticSystem.AddCityUsageAccumulatorWriter(CityProductionStatisticSystem.CityResourceUsage.Consumer.ServiceUpkeep, base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
	}

	public static UpkeepModifierData GetUpkeepModifier(Resource resource, NativeList<UpkeepModifierData> upkeepModifiers)
	{
		foreach (UpkeepModifierData item in upkeepModifiers)
		{
			if (item.m_Resource == resource)
			{
				return item;
			}
		}
		return new UpkeepModifierData
		{
			m_Resource = resource,
			m_Multiplier = 1f
		};
	}

	public static byte GetResourceAvailability(NativeList<ServiceUpkeepData> upkeeps, DynamicBuffer<Game.Economy.Resources> resources, NativeArray<int> storageTargets)
	{
		byte b = byte.MaxValue;
		foreach (ServiceUpkeepData item in upkeeps)
		{
			Resource resource = item.m_Upkeep.m_Resource;
			int num = storageTargets[EconomyUtils.GetResourceIndex(resource)];
			if (num > 0)
			{
				int resources2 = EconomyUtils.GetResources(resource, resources);
				byte b2 = (byte)math.clamp(math.ceil(255f * (float)resources2 / (float)num), 0f, 255f);
				if (b2 < b)
				{
					b = b2;
				}
			}
		}
		return b;
	}

	public static int CalculateUpkeep(int amount, Entity prefabEntity, Entity budgetEntity, EntityManager entityManager)
	{
		Entity entity = Entity.Null;
		if (entityManager.TryGetComponent<ServiceObjectData>(prefabEntity, out var component))
		{
			entity = component.m_Service;
		}
		int num = 100;
		if (entityManager.TryGetBuffer(budgetEntity, isReadOnly: true, out DynamicBuffer<ServiceBudgetData> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				if (buffer[i].m_Service == entity)
				{
					num = buffer[i].m_Budget;
				}
			}
		}
		return (int)math.round((float)amount * ((float)num / 100f));
	}

	public static void GetUpkeepModifierData(NativeList<UpkeepModifierData> upkeepModifierList, BufferLookup<InstalledUpgrade> installedUpgrades, ComponentLookup<PrefabRef> prefabs, BufferLookup<UpkeepModifierData> upkeepModifiers, Entity entity)
	{
		if (installedUpgrades.TryGetBuffer(entity, out var bufferData))
		{
			UpgradeUtils.CombineStats(upkeepModifierList, bufferData, ref prefabs, ref upkeepModifiers);
		}
	}

	public static bool IsMaterialResource(ComponentLookup<ResourceData> resourceDatas, ResourcePrefabs resourcePrefabs, ResourceStack upkeep)
	{
		return resourceDatas[resourcePrefabs[upkeep.m_Resource]].m_Weight > 0f;
	}

	public static int GetUpkeepOfEmployeeWage(BufferLookup<Employee> employeeBufs, Entity entity, EconomyParameterData economyParameterData, bool mainBuildingDisabled)
	{
		if (mainBuildingDisabled)
		{
			return 0;
		}
		int num = 0;
		if (employeeBufs.TryGetBuffer(entity, out var bufferData))
		{
			for (int i = 0; i < bufferData.Length; i++)
			{
				num += economyParameterData.GetWage(bufferData[i].m_Level, cityServiceJob: true);
			}
		}
		return num;
	}

	public static void GetUpkeepWithUsageScale(NativeList<ServiceUpkeepData> totalUpkeepDatas, BufferLookup<ServiceUpkeepData> serviceUpkeepDatas, BufferLookup<InstalledUpgrade> installedUpgradeBufs, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<ServiceUsage> serviceUsages, Entity entity, Entity prefab, bool mainBuildingDisabled)
	{
		if (serviceUpkeepDatas.TryGetBuffer(prefab, out var bufferData))
		{
			foreach (ServiceUpkeepData item in bufferData)
			{
				ServiceUpkeepData value = item;
				if (value.m_ScaleWithUsage && serviceUsages.TryGetComponent(entity, out var componentData))
				{
					totalUpkeepDatas.Add(value.ApplyServiceUsage(componentData.m_Usage));
				}
				else
				{
					totalUpkeepDatas.Add(in value);
				}
			}
		}
		if (!installedUpgradeBufs.TryGetBuffer(entity, out var bufferData2))
		{
			return;
		}
		foreach (InstalledUpgrade item2 in bufferData2)
		{
			bool flag = BuildingUtils.CheckOption(item2, BuildingOption.Inactive);
			if (!prefabRefs.TryGetComponent(item2.m_Upgrade, out var componentData2) || !serviceUpkeepDatas.TryGetBuffer(componentData2.m_Prefab, out var bufferData3))
			{
				continue;
			}
			for (int i = 0; i < bufferData3.Length; i++)
			{
				ServiceUpkeepData combineData = bufferData3[i];
				if (combineData.m_Upkeep.m_Resource == Resource.Money)
				{
					if (!mainBuildingDisabled && flag)
					{
						combineData.m_Upkeep.m_Amount = (combineData.m_Upkeep.m_Amount + 5) / 10;
					}
				}
				else if (flag)
				{
					continue;
				}
				if (combineData.m_ScaleWithUsage && serviceUsages.TryGetComponent(item2.m_Upgrade, out var componentData3))
				{
					UpgradeUtils.CombineStats(totalUpkeepDatas, combineData.ApplyServiceUsage(componentData3.m_Usage));
				}
				else
				{
					UpgradeUtils.CombineStats(totalUpkeepDatas, combineData);
				}
			}
		}
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
	public CityServiceUpkeepSystem()
	{
	}
}
