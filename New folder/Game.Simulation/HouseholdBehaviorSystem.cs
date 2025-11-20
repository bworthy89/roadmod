using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class HouseholdBehaviorSystem : GameSystemBase
{
	[BurstCompile]
	private struct HouseholdTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Household> m_HouseholdType;

		public ComponentTypeHandle<HouseholdNeed> m_HouseholdNeedType;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

		public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<TouristHousehold> m_TouristHouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<CommuterHousehold> m_CommuterHouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<LodgingSeeker> m_LodgingSeekerType;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		[ReadOnly]
		public ComponentLookup<PropertySeeker> m_PropertySeekers;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> m_LodgingProviders;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenDatas;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public RandomSeed m_RandomSeed;

		public float m_ResourceDemandPerCitizenMultiplier;

		public EconomyParameterData m_EconomyParameters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public uint m_UpdateFrameIndex;

		public uint m_FrameIndex;

		public Entity m_City;

		private bool NeedsCar(int spendableMoney, int familySize, int cars, ref Unity.Mathematics.Random random)
		{
			if (spendableMoney > kCarBuyingMinimumMoney)
			{
				return (double)random.NextFloat() < (double)((0f - math.log((float)cars + 0.1f)) / 10f) + 0.1;
			}
			return false;
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Household> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdType);
			NativeArray<HouseholdNeed> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdNeedType);
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourceType);
			NativeArray<TouristHousehold> nativeArray4 = chunk.GetNativeArray(ref m_TouristHouseholdType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			int population = m_Populations[m_City].m_Population;
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Household household = nativeArray2[i];
				DynamicBuffer<HouseholdCitizen> citizens = bufferAccessor[i];
				if (m_FrameIndex - household.m_LastDayFrameIndex > 262144)
				{
					household.m_ShoppedValueLastDay = household.m_ShoppedValuePerDay;
					household.m_ShoppedValuePerDay = 0u;
					household.m_MoneySpendOnBuildingLevelingLastDay = 0;
					household.m_LastDayFrameIndex = m_FrameIndex;
				}
				if (citizens.Length == 0)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Deleted));
					continue;
				}
				bool flag = true;
				int num = 0;
				for (int j = 0; j < citizens.Length; j++)
				{
					num += m_CitizenDatas[citizens[j].m_Citizen].Happiness;
					if (m_CitizenDatas[citizens[j].m_Citizen].GetAge() >= CitizenAge.Adult)
					{
						flag = false;
					}
				}
				num /= citizens.Length;
				bool flag2 = (float)random.NextInt(1000) < -53.35f * (float)num + Mathf.Sqrt(95.96f * (float)num * (float)num + 1013f * (float)num + 6576f) * 5.408f - 298.5f;
				bool flag3 = chunk.Has<HomelessHousehold>();
				DynamicBuffer<Game.Economy.Resources> resources = bufferAccessor2[i];
				int householdTotalWealth = EconomyUtils.GetHouseholdTotalWealth(household, resources);
				int num2 = (household.m_SalaryLastDay = EconomyUtils.GetHouseholdIncome(citizens, ref m_Workers, ref m_CitizenDatas, ref m_HealthProblems, ref m_EconomyParameters, m_TaxRates));
				MoveAwayReason moveAwayReason = (flag ? MoveAwayReason.NoAdults : (flag2 ? MoveAwayReason.NotHappy : ((householdTotalWealth + num2 < -1000) ? MoveAwayReason.NoMoney : MoveAwayReason.None)));
				if (moveAwayReason != MoveAwayReason.None)
				{
					CitizenUtils.HouseholdMoveAway(m_CommandBuffer, unfilteredChunkIndex, entity, moveAwayReason);
					continue;
				}
				if (!flag3)
				{
					if (!m_PropertyRenters.HasComponent(entity) || m_PropertyRenters[entity].m_Property == Entity.Null)
					{
						if ((household.m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None)
						{
							m_CommandBuffer.AddComponent<HomelessHousehold>(unfilteredChunkIndex, entity);
						}
					}
					else
					{
						PropertyRenter propertyRenter = m_PropertyRenters[entity];
						UpdateHouseholdNeed(chunk, unfilteredChunkIndex, nativeArray3, i, ref household, householdTotalWealth, citizens, nativeArray4, entity, propertyRenter, resources, population, ref random);
					}
				}
				else
				{
					EconomyUtils.AddResources(Resource.Money, -1, resources);
				}
				if (!chunk.Has(ref m_TouristHouseholdType) && !chunk.Has(ref m_CommuterHouseholdType) && !m_PropertySeekers.IsComponentEnabled(nativeArray[i]))
				{
					Entity householdHomeBuilding = BuildingUtils.GetHouseholdHomeBuilding(entity, ref m_PropertyRenters, ref m_HomelessHouseholds);
					if (householdHomeBuilding == Entity.Null || !m_RenterBufs.HasBuffer(householdHomeBuilding))
					{
						m_CommandBuffer.SetComponentEnabled<PropertySeeker>(unfilteredChunkIndex, nativeArray[i], value: true);
					}
					else
					{
						int num3 = math.clamp(Mathf.RoundToInt(0.06f * (float)population), 64, 1024);
						if (flag3)
						{
							num3 /= 10;
						}
						if (random.NextInt(num3) == 0)
						{
							m_CommandBuffer.SetComponentEnabled<PropertySeeker>(unfilteredChunkIndex, nativeArray[i], value: true);
						}
					}
				}
				nativeArray2[i] = household;
			}
		}

		private void UpdateHouseholdNeed(ArchetypeChunk chunk, int unfilteredChunkIndex, NativeArray<HouseholdNeed> householdNeeds, int i, ref Household household, int totalWealth, DynamicBuffer<HouseholdCitizen> citizens, NativeArray<TouristHousehold> touristHouseholds, Entity entity, PropertyRenter propertyRenter, DynamicBuffer<Game.Economy.Resources> resources, int population, ref Unity.Mathematics.Random random)
		{
			HouseholdNeed value = householdNeeds[i];
			if (household.m_Resources > 0)
			{
				float num = GetConsumptionMultiplier(m_EconomyParameters.m_ResourceConsumptionMultiplier, totalWealth) * m_EconomyParameters.m_ResourceConsumptionPerCitizen * (float)citizens.Length;
				if (chunk.Has(ref m_TouristHouseholdType))
				{
					num *= m_EconomyParameters.m_TouristConsumptionMultiplier;
					if (!chunk.Has(ref m_LodgingSeekerType))
					{
						TouristHousehold value2 = touristHouseholds[i];
						if (value2.m_Hotel.Equals(Entity.Null))
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(LodgingSeeker));
						}
						else if (!m_LodgingProviders.HasComponent(value2.m_Hotel))
						{
							value2.m_Hotel = Entity.Null;
							touristHouseholds[i] = value2;
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(LodgingSeeker));
						}
					}
				}
				int num2 = MathUtils.RoundToIntRandom(ref random, num);
				household.m_ConsumptionPerDay = (short)math.min(32767, kUpdatesPerDay * num2);
				household.m_Resources = math.max(household.m_Resources - num2, 0);
				return;
			}
			household.m_Resources = 0;
			household.m_ConsumptionPerDay = 0;
			if (value.m_Resource != Resource.NoResource)
			{
				return;
			}
			int householdSpendableMoney = EconomyUtils.GetHouseholdSpendableMoney(household, resources, ref m_RenterBufs, ref m_ConsumptionDatas, ref m_PrefabRefs, propertyRenter);
			if (householdSpendableMoney < kMinimumShoppingMoney)
			{
				value.m_Amount = 0;
				value.m_Resource = Resource.NoResource;
				householdNeeds[i] = value;
				return;
			}
			int num3 = 0;
			if (m_OwnedVehicles.HasBuffer(entity))
			{
				num3 = m_OwnedVehicles[entity].Length;
			}
			ResourceIterator iterator = ResourceIterator.GetIterator();
			int num4 = 0;
			while (iterator.Next())
			{
				num4 += GetResourceShopWeightWithAge(householdSpendableMoney, iterator.resource, m_ResourcePrefabs, ref m_ResourceDatas, num3, leisureIncluded: false, citizens, ref m_CitizenDatas);
			}
			int num5 = random.NextInt(num4);
			iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceShopWeightWithAge = GetResourceShopWeightWithAge(householdSpendableMoney, iterator.resource, m_ResourcePrefabs, ref m_ResourceDatas, num3, leisureIncluded: false, citizens, ref m_CitizenDatas);
				num4 -= resourceShopWeightWithAge;
				if (resourceShopWeightWithAge <= 0 || num4 > num5)
				{
					continue;
				}
				if (!EconomyUtils.IsOfficeResource(iterator.resource))
				{
					int num6 = math.min(kMaxShoppingPossibility, Mathf.RoundToInt(200f / math.max(1f, math.sqrt(m_EconomyParameters.m_TrafficReduction * (float)population))));
					if (household.m_ShoppedValuePerDay != 0)
					{
						num6 /= 10;
					}
					if (random.NextInt(100) > num6)
					{
						break;
					}
				}
				if (iterator.resource == Resource.Vehicles && NeedsCar(householdSpendableMoney, citizens.Length, num3, ref random))
				{
					value.m_Resource = Resource.Vehicles;
					value.m_Amount = kCarAmount;
					householdNeeds[i] = value;
					break;
				}
				value.m_Resource = iterator.resource;
				float marketPrice = EconomyUtils.GetMarketPrice(m_ResourceDatas[m_ResourcePrefabs[iterator.resource]]);
				value.m_Amount = math.clamp((int)((float)householdSpendableMoney / marketPrice), 0, kMaxHouseholdNeedAmount);
				value.m_Amount = (int)((float)value.m_Amount * m_ResourceDemandPerCitizenMultiplier);
				if (value.m_Amount > 0)
				{
					householdNeeds[i] = value;
				}
				break;
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

		public ComponentTypeHandle<Household> __Game_Citizens_Household_RW_ComponentTypeHandle;

		public ComponentTypeHandle<HouseholdNeed> __Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle;

		public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

		public ComponentTypeHandle<TouristHousehold> __Game_Citizens_TouristHousehold_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommuterHousehold> __Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LodgingSeeker> __Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertySeeker> __Game_Agents_PropertySeeker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Household_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Household>();
			__Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdNeed>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>();
			__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TouristHousehold>();
			__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommuterHousehold>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingSeeker>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Agents_PropertySeeker_RO_ComponentLookup = state.GetComponentLookup<PropertySeeker>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Companies_LodgingProvider_RO_ComponentLookup = state.GetComponentLookup<LodgingProvider>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
		}
	}

	public static readonly int kCarAmount = 50;

	public static readonly int kUpdatesPerDay = 256;

	public static readonly int kMaxShoppingPossibility = 80;

	public static readonly int kMaxHouseholdNeedAmount = 2000;

	public static readonly int kCarBuyingMinimumMoney = 10000;

	public static readonly int kMinimumShoppingMoney = 1000;

	private EntityQuery m_HouseholdGroup;

	private EntityQuery m_EconomyParameterGroup;

	private EntityQuery m_GameModeSettingQuery;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ResourceSystem m_ResourceSystem;

	private TaxSystem m_TaxSystem;

	private CitySystem m_CitySystem;

	private float m_ResourceDemandPerCitizenMultiplier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			m_ResourceDemandPerCitizenMultiplier = 1f;
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable)
		{
			m_ResourceDemandPerCitizenMultiplier = singleton.m_ResourceDemandPerCitizenMultiplier;
		}
		else
		{
			m_ResourceDemandPerCitizenMultiplier = 1f;
		}
	}

	public static float GetLastCommutePerCitizen(DynamicBuffer<HouseholdCitizen> householdCitizens, ComponentLookup<Worker> workers)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < householdCitizens.Length; i++)
		{
			Entity citizen = householdCitizens[i].m_Citizen;
			if (workers.HasComponent(citizen))
			{
				num2 += workers[citizen].m_LastCommuteTime;
			}
			num += 1f;
		}
		return num2 / num;
	}

	public static float GetConsumptionMultiplier(float2 parameter, int householdWealth)
	{
		return parameter.x + parameter.y * math.smoothstep(0f, 1f, (float)(math.max(0, householdWealth) + 1000) / 6000f);
	}

	public static bool GetFreeCar(Entity household, BufferLookup<OwnedVehicle> ownedVehicles, ComponentLookup<Game.Vehicles.PersonalCar> personalCars, ref Entity car)
	{
		if (ownedVehicles.HasBuffer(household))
		{
			DynamicBuffer<OwnedVehicle> dynamicBuffer = ownedVehicles[household];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				car = dynamicBuffer[i].m_Vehicle;
				if (personalCars.HasComponent(car) && personalCars[car].m_Keeper.Equals(Entity.Null))
				{
					return true;
				}
			}
		}
		car = Entity.Null;
		return false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EconomyParameterGroup = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_HouseholdGroup = GetEntityQuery(ComponentType.ReadWrite<Household>(), ComponentType.ReadWrite<HouseholdNeed>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.ReadOnly<Game.Economy.Resources>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		m_ResourceDemandPerCitizenMultiplier = 1f;
		RequireForUpdate(m_HouseholdGroup);
		RequireForUpdate(m_EconomyParameterGroup);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		HouseholdTickJob jobData = new HouseholdTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdNeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdNeed_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TouristHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TouristHousehold_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CommuterHouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CommuterHousehold_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_LodgingSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_LodgingSeeker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_EconomyParameters = m_EconomyParameterGroup.GetSingleton<EconomyParameterData>(),
			m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertySeekers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_PropertySeeker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LodgingProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConsumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_RandomSeed = RandomSeed.Next(),
			m_ResourceDemandPerCitizenMultiplier = m_ResourceDemandPerCitizenMultiplier,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_FrameIndex = m_SimulationSystem.frameIndex,
			m_City = m_CitySystem.City
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_HouseholdGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
		m_TaxSystem.AddReader(base.Dependency);
	}

	public static int GetAgeWeight(ResourceData resourceData, DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Citizen> citizenDatas)
	{
		int num = 0;
		for (int i = 0; i < citizens.Length; i++)
		{
			Entity citizen = citizens[i].m_Citizen;
			num = citizenDatas[citizen].GetAge() switch
			{
				CitizenAge.Child => num + resourceData.m_ChildWeight, 
				CitizenAge.Teen => num + resourceData.m_TeenWeight, 
				CitizenAge.Elderly => num + resourceData.m_ElderlyWeight, 
				_ => num + resourceData.m_AdultWeight, 
			};
		}
		return num;
	}

	public static int GetResourceShopWeightWithAge(int wealth, Resource resource, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, int carCount, bool leisureIncluded, DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Citizen> citizenDatas)
	{
		ResourceData resourceData = resourceDatas[resourcePrefabs[resource]];
		return GetResourceShopWeightWithAge(wealth, resourceData, carCount, leisureIncluded, citizens, ref citizenDatas);
	}

	public static int GetResourceShopWeightWithAge(int wealth, ResourceData resourceData, int carCount, bool leisureIncluded, DynamicBuffer<HouseholdCitizen> citizens, ref ComponentLookup<Citizen> citizenDatas)
	{
		float num = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_BaseConsumption : 0f);
		num += (float)(carCount * resourceData.m_CarConsumption);
		float xMin = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_WealthModifier : 0f);
		float num2 = GetAgeWeight(resourceData, citizens, ref citizenDatas);
		return Mathf.RoundToInt(100f * num2 * num * math.smoothstep(xMin, 1f, math.max(0.01f, ((float)wealth + 5000f) / 10000f)));
	}

	public static int GetWeight(int wealth, Resource resource, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, int carCount, bool leisureIncluded)
	{
		ResourceData resourceData = resourceDatas[resourcePrefabs[resource]];
		return GetWeight(wealth, resourceData, carCount, leisureIncluded);
	}

	public static int GetWeight(int wealth, ResourceData resourceData, int carCount, bool leisureIncluded)
	{
		float num = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_BaseConsumption : 0f) + (float)(carCount * resourceData.m_CarConsumption);
		float xMin = ((leisureIncluded || !resourceData.m_IsLeisure) ? resourceData.m_WealthModifier : 0f);
		return Mathf.RoundToInt(num * math.smoothstep(xMin, 1f, math.clamp(((float)wealth + 5000f) / 10000f, 0.1f, 0.9f)));
	}

	public static int GetHighestEducation(DynamicBuffer<HouseholdCitizen> citizenBuffer, ref ComponentLookup<Citizen> citizens)
	{
		int num = 0;
		for (int i = 0; i < citizenBuffer.Length; i++)
		{
			Entity citizen = citizenBuffer[i].m_Citizen;
			if (citizens.HasComponent(citizen))
			{
				Citizen citizen2 = citizens[citizen];
				CitizenAge age = citizen2.GetAge();
				if (age == CitizenAge.Teen || age == CitizenAge.Adult)
				{
					num = math.max(num, citizen2.GetEducationLevel());
				}
			}
		}
		return num;
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
	public HouseholdBehaviorSystem()
	{
	}
}
