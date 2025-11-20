using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Objects;
using Game.Policies;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Routes;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Achievements;

[CompilerGenerated]
public class AchievementTriggerSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public class ProgressBuffer
	{
		private AchievementId m_Achievement;

		private int m_IncrementStep;

		private IndicateType m_Type;

		public int m_Progress;

		public ProgressBuffer(AchievementId achievement, int incrementStep, IndicateType type)
		{
			m_Achievement = achievement;
			m_IncrementStep = incrementStep;
			m_Type = type;
			m_Progress = 0;
		}

		public void AddProgress(int progress)
		{
			m_Progress += progress;
			if (m_Type == IndicateType.Increment)
			{
				int num = m_Progress / m_IncrementStep;
				if (num > 0)
				{
					m_Progress %= m_IncrementStep;
					PlatformManager.instance.IndicateAchievementProgress(m_Achievement, num * m_IncrementStep, m_Type);
				}
			}
			else if (m_Type == IndicateType.Absolute)
			{
				int num2 = m_Progress / m_IncrementStep;
				PlatformManager.instance.IndicateAchievementProgress(m_Achievement, num2 * m_IncrementStep, m_Type);
			}
		}
	}

	public class UserDataProgressBuffer : ProgressBuffer, IDisposable
	{
		private string m_ID;

		private static byte[] sBuffer = new byte[4];

		public UserDataProgressBuffer(AchievementId achievement, int incrementStep, IndicateType type, string id)
			: base(achievement, incrementStep, type)
		{
			m_ID = id;
			Sync();
		}

		private void Sync()
		{
			try
			{
				if (PlatformManager.instance.UserDataLoad(m_ID) && PlatformManager.instance.UserDataLoad(m_ID, sBuffer))
				{
					m_Progress = BinaryPrimitives.ReadInt32LittleEndian(sBuffer);
				}
			}
			catch (Exception exception)
			{
				sLog.Error(exception);
			}
		}

		private void Store()
		{
			try
			{
				BinaryPrimitives.WriteInt32LittleEndian(sBuffer, m_Progress);
				PlatformManager.instance.UserDataStore(m_ID, sBuffer);
			}
			catch (Exception exception)
			{
				sLog.Error(exception);
			}
		}

		public void Dispose()
		{
			Store();
		}
	}

	[BurstCompile]
	private struct ProcessDependencyDataJob : IJob
	{
		[ReadOnly]
		public NativeArray<long> m_ProducedResources;

		public NativeArray<long> m_ResourceProducedArray;

		public void Execute()
		{
			m_ResourceProducedArray.CopyFrom(m_ProducedResources);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Followed> __Game_Citizens_Followed_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<AchievementFilterData> __Game_Prefabs_AchievementFilterData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Policy> __Game_Policies_Policy_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_OfficeBuilding_RO_ComponentLookup = state.GetComponentLookup<OfficeBuilding>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_Followed_RO_ComponentLookup = state.GetComponentLookup<Followed>(isReadOnly: true);
			__Game_Prefabs_AchievementFilterData_RO_BufferLookup = state.GetBufferLookup<AchievementFilterData>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(isReadOnly: true);
			__Game_Policies_Policy_RO_BufferLookup = state.GetBufferLookup<Policy>(isReadOnly: true);
		}
	}

	private static ILog sLog = LogManager.GetLogger("Platforms");

	private ToolSystem m_ToolSystem;

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private SimulationSystem m_SimulationSystem;

	private ClimateSystem m_ClimateSystem;

	private TimeSystem m_TimeSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_CreatedObjectQuery;

	private EntityQuery m_ObjectAchievementQuery;

	private EntityQuery m_UnlockQuery;

	private EntityQuery m_ParkQuery;

	private EntityQuery m_CreatedParkQuery;

	private EntityQuery m_LockedServiceQuery;

	private EntityQuery m_ServiceQuery;

	private EntityQuery m_LockedBuildingQuery;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_TransportLineQuery;

	private EntityQuery m_CreatedTransportLineQuery;

	private EntityQuery m_UniqueServiceBuildingPrefabQuery;

	private EntityQuery m_UniqueServiceBuildingQuery;

	private EntityQuery m_CreatedUniqueServiceBuildingQuery;

	private EntityQuery m_PolicyModificationQuery;

	private EntityQuery m_DistrictQuery;

	private EntityQuery m_ServiceDistrictBuildingQuery;

	private EntityQuery m_FossilEnergyProducersQuery;

	private EntityQuery m_RenewableEnergyProducersQuery;

	private EntityQuery m_EnergyProducersQuery;

	private EntityQuery m_WaterPumpingStationQuery;

	private EntityQuery m_ResidentialBuildingsQuery;

	private EntityQuery m_CommercialBuildingsQuery;

	private EntityQuery m_IndustrialBuildingsQuery;

	private EntityQuery m_FollowedCitizensQuery;

	private EntityQuery m_InfoviewQuery;

	private EntityQuery m_CreatedUniqueBuildingQuery;

	private EntityQuery m_UniqueBuildingQuery;

	private EntityQuery m_PlantQuery;

	private EntityQuery m_CreatedPlantQuery;

	private EntityQuery m_TimeDataQuery;

	private EntityQuery m_TimeSettingsQuery;

	private EntityQuery m_ProduceResourceCompaniesQuery;

	private EntityQuery m_CreatedAggregateElementQuery;

	private EntityQuery m_AggregateElementQuery;

	public NativeCounter m_PatientsTreatedCounter;

	public NativeCounter m_ProducedFishCounter;

	public NativeCounter m_OffshoreOilProduceCounter;

	private JobHandle m_TransportWriteDeps;

	private NativeQueue<TransportedResource> m_TransportedResourceQueue;

	private int m_CachedPatientsTreatedCount;

	private int m_CachedPopulationCount;

	private int m_CachedHappiness;

	private int m_CachedAttractiveness;

	private int m_CachedTouristCount;

	private bool m_CheckUnlocks;

	private uint m_LastCheckFrameIndex;

	private static readonly int kMinCityEffectPopulation = 1000;

	private static readonly int kAllSmilesHappiness = 75;

	private static readonly int kThisIsNotMyHappyPlaceHappiness = 25;

	private static readonly int kSimplyIrresistibleAttractiveness = 90;

	private static readonly int kZeroEmissionMinProduction = 5000000;

	private static readonly int kColossalGardenerLimit = 100;

	private static readonly int kTheDeepEndLoanAmount = 200000;

	private HashSet<InfoviewPrefab> m_ViewedInfoviews = new HashSet<InfoviewPrefab>();

	private Dictionary<AchievementId, int> m_IncrementalObjectAchievementProgress = new Dictionary<AchievementId, int>();

	private List<AchievementId> m_AbsoluteObjectAchievements = new List<AchievementId>();

	public ProgressBuffer m_LittleBitOfTLCBuffer;

	public ProgressBuffer m_HowMuchIsTheFishBuffer;

	public ProgressBuffer m_ADifferentPlatformerBuffer;

	public UserDataProgressBuffer m_SquasherDownerBuffer;

	public UserDataProgressBuffer m_ShipItBuffer;

	private TypeHandle __TypeHandle;

	public NativeQueue<TransportedResource> GetTransportedResourceQueue()
	{
		return m_TransportedResourceQueue;
	}

	public void AddWriter(JobHandle writer)
	{
		m_TransportWriteDeps = JobHandle.CombineDependencies(m_TransportWriteDeps, writer);
	}

	public bool GetDebugData(AchievementId achievement, out string data)
	{
		if (achievement == Achievements.ALittleBitofTLC)
		{
			data = $"{m_LittleBitOfTLCBuffer?.m_Progress ?? 0}";
			return true;
		}
		if (achievement == Achievements.HowMuchIsTheFish)
		{
			data = $"{m_HowMuchIsTheFishBuffer?.m_Progress ?? 0}";
			return true;
		}
		if (achievement == Achievements.ADifferentPlatformer)
		{
			data = $"{m_ADifferentPlatformerBuffer?.m_Progress ?? 0}";
			return true;
		}
		if (achievement == Achievements.OneofEverything)
		{
			data = $"{CountUniqueServiceBuildings()}/{CountUniqueServiceBuildingPrefabs()}";
			return true;
		}
		data = string.Empty;
		return false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LittleBitOfTLCBuffer = new ProgressBuffer(Achievements.ALittleBitofTLC, 1000, IndicateType.Absolute);
		m_SquasherDownerBuffer = new UserDataProgressBuffer(Achievements.SquasherDowner, 10, IndicateType.Increment, "SquasherDowner");
		m_PatientsTreatedCounter = new NativeCounter(Allocator.Persistent);
		m_ProducedFishCounter = new NativeCounter(Allocator.Persistent);
		m_OffshoreOilProduceCounter = new NativeCounter(Allocator.Persistent);
		m_TransportedResourceQueue = new NativeQueue<TransportedResource>(Allocator.Persistent);
		m_HowMuchIsTheFishBuffer = new ProgressBuffer(Achievements.HowMuchIsTheFish, 100000, IndicateType.Absolute);
		m_ADifferentPlatformerBuffer = new ProgressBuffer(Achievements.ADifferentPlatformer, 200000, IndicateType.Absolute);
		m_ShipItBuffer = new UserDataProgressBuffer(Achievements.ShipIt, 1000000, IndicateType.Increment, "ShipIt");
		m_CachedPatientsTreatedCount = 0;
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CreatedObjectQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectAchievement>(), ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ObjectAchievementQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectAchievement>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_UnlockQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_ParkQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Park>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Extension>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CreatedParkQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Park>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Extension>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_LockedServiceQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceData>(), ComponentType.ReadOnly<Locked>());
		m_ServiceQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceData>());
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingData>());
		m_LockedBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<Locked>());
		m_TransportLineQuery = GetEntityQuery(ComponentType.ReadOnly<TransportLine>(), ComponentType.ReadOnly<Route>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CreatedTransportLineQuery = GetEntityQuery(ComponentType.ReadOnly<TransportLine>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_UniqueServiceBuildingPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<UniqueObjectData>(), ComponentType.ReadOnly<CollectedServiceBuildingBudgetData>(), ComponentType.ReadOnly<PrefabData>());
		m_UniqueServiceBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_CreatedUniqueServiceBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PolicyModificationQuery = GetEntityQuery(ComponentType.ReadOnly<Modify>());
		m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>(), ComponentType.ReadOnly<Policy>());
		m_ServiceDistrictBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ServiceDistrict>(), ComponentType.Exclude<Deleted>());
		m_FossilEnergyProducersQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityProducer>(), ComponentType.Exclude<RenewableElectricityProduction>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_RenewableEnergyProducersQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityProducer>(), ComponentType.ReadOnly<RenewableElectricityProduction>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_EnergyProducersQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityProducer>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_WaterPumpingStationQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.WaterPumpingStation>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ResidentialBuildingsQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_CommercialBuildingsQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<CommercialProperty>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_IndustrialBuildingsQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<IndustrialProperty>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_FollowedCitizensQuery = GetEntityQuery(ComponentType.ReadOnly<Followed>(), ComponentType.ReadOnly<Citizen>());
		m_InfoviewQuery = GetEntityQuery(ComponentType.ReadOnly<InfoviewData>());
		m_UniqueBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CreatedUniqueBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.UniqueObject>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_PlantQuery = GetEntityQuery(ComponentType.ReadOnly<Plant>(), ComponentType.Exclude<Owner>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CreatedPlantQuery = GetEntityQuery(ComponentType.ReadOnly<Plant>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Owner>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ProduceResourceCompaniesQuery = GetEntityQuery(ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CreatedAggregateElementQuery = GetEntityQuery(ComponentType.ReadOnly<AggregateElement>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_AggregateElementQuery = GetEntityQuery(ComponentType.ReadOnly<AggregateElement>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_TimeSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		ToolSystem toolSystem = m_ToolSystem;
		toolSystem.EventInfoviewChanged = (Action<InfoviewPrefab, InfoviewPrefab>)Delegate.Combine(toolSystem.EventInfoviewChanged, new Action<InfoviewPrefab, InfoviewPrefab>(OnInfoviewChanged));
	}

	private void Reset()
	{
		m_CachedHappiness = 50;
		m_CachedAttractiveness = 0;
		m_CheckUnlocks = true;
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		Reset();
		PlatformManager.instance.achievementsEnabled &= m_CityConfigurationSystem.usedMods.Count == 0 && !m_CityConfigurationSystem.unlimitedMoney && !m_CityConfigurationSystem.unlockAll && !m_CityConfigurationSystem.unlockMapTiles;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ToolSystem.actionMode.IsEditor() && PlatformManager.instance.achievementsEnabled && GameManager.instance.state != GameManager.State.Loading && GameManager.instance.gameMode.IsGameOrEditor())
		{
			CheckInGameAchievements();
		}
	}

	private int CountAbsoluteObjectAchievementProgress(AchievementId achID)
	{
		float num = 0f;
		NativeArray<Entity> nativeArray = m_ObjectAchievementQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (!base.EntityManager.TryGetComponent<PrefabRef>(nativeArray[i], out var component) || !base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<ObjectAchievementData> buffer))
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < buffer.Length; j++)
			{
				if (buffer[j].m_ID == achID)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			if (achID == Achievements.Pierfect || achID == Achievements.ItsPronouncedKey)
			{
				if (base.EntityManager.TryGetComponent<Curve>(nativeArray[i], out var component2))
				{
					num += component2.m_Length;
				}
			}
			else
			{
				num += 1f;
			}
		}
		nativeArray.Dispose();
		return (int)num;
	}

	private void CheckInGameAchievements()
	{
		if (!m_CreatedObjectQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_CreatedObjectQuery.ToEntityArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (!base.EntityManager.TryGetComponent<PrefabRef>(nativeArray[i], out var component) || !base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<ObjectAchievementData> buffer))
				{
					continue;
				}
				for (int j = 0; j < buffer.Length; j++)
				{
					if (buffer[j].m_BypassCounter)
					{
						PlatformManager.instance.UnlockAchievement(buffer[j].m_ID);
					}
					else if (buffer[j].m_AbsoluteCounter)
					{
						if (!m_AbsoluteObjectAchievements.Contains(buffer[j].m_ID))
						{
							m_AbsoluteObjectAchievements.Add(buffer[j].m_ID);
						}
					}
					else
					{
						int valueOrDefault = m_IncrementalObjectAchievementProgress.GetValueOrDefault(buffer[j].m_ID, 0);
						valueOrDefault++;
						m_IncrementalObjectAchievementProgress[buffer[j].m_ID] = valueOrDefault;
					}
				}
			}
			foreach (KeyValuePair<AchievementId, int> item in m_IncrementalObjectAchievementProgress)
			{
				if (PlatformManager.instance.GetAchievement(item.Key, out var achievement))
				{
					int b = achievement.maxProgress - achievement.progress;
					int value = Mathf.Min(item.Value, b);
					PlatformManager.instance.IndicateAchievementProgress(item.Key, value, IndicateType.Increment);
				}
			}
			foreach (AchievementId item2 in m_AbsoluteObjectAchievements)
			{
				if (PlatformManager.instance.GetAchievement(item2, out var achievement2) && !achievement2.achieved)
				{
					int num = CountAbsoluteObjectAchievementProgress(item2);
					num = ((achievement2.maxProgress > 0) ? Mathf.Min(num, achievement2.maxProgress) : num);
					PlatformManager.instance.IndicateAchievementProgress(item2, num);
				}
			}
			m_IncrementalObjectAchievementProgress.Clear();
			m_AbsoluteObjectAchievements.Clear();
			nativeArray.Dispose();
		}
		int value2;
		if (!m_CreatedParkQuery.IsEmptyIgnoreFilter)
		{
			value2 = CountParks();
			PlatformManager.instance.IndicateAchievementProgress(Achievements.Groundskeeper, value2);
		}
		if (m_CheckUnlocks || !m_UnlockQuery.IsEmptyIgnoreFilter)
		{
			m_CheckUnlocks = false;
			CheckUnlockingAchievements();
		}
		if (base.EntityManager.TryGetComponent<Loan>(m_CitySystem.City, out var component2) && component2.m_LastModified >= m_LastCheckFrameIndex)
		{
			PlatformManager.instance.IndicateAchievementProgress(Achievements.TheDeepEnd, Mathf.Min(component2.m_Amount, kTheDeepEndLoanAmount));
		}
		if (base.EntityManager.TryGetComponent<Population>(m_CitySystem.City, out var component3))
		{
			int num2 = ((component3.m_Population >= 10000) ? 10000 : 1000);
			int num3 = component3.m_Population / num2 * num2;
			if (num3 != m_CachedPopulationCount)
			{
				m_CachedPopulationCount = num3;
				PlatformManager.instance.IndicateAchievementProgress(Achievements.SixFigures, num3);
			}
		}
		if (!m_CreatedTransportLineQuery.IsEmptyIgnoreFilter || !m_PolicyModificationQuery.IsEmptyIgnoreFilter)
		{
			value2 = 0;
			NativeArray<Route> nativeArray2 = m_TransportLineQuery.ToComponentDataArray<Route>(Allocator.TempJob);
			try
			{
				for (int k = 0; k < nativeArray2.Length; k++)
				{
					if (!RouteUtils.CheckOption(nativeArray2[k], RouteOption.Inactive))
					{
						value2++;
					}
				}
			}
			finally
			{
				nativeArray2.Dispose();
			}
			PlatformManager.instance.IndicateAchievementProgress(Achievements.GoAnywhere, value2);
			PlatformManager.instance.IndicateAchievementProgress(Achievements.Spiderwebbing, value2);
		}
		if (component3.m_Population >= kMinCityEffectPopulation)
		{
			if (m_CachedHappiness < kAllSmilesHappiness && component3.m_AverageHappiness >= kAllSmilesHappiness)
			{
				PlatformManager.instance.UnlockAchievement(Achievements.AllSmiles);
			}
			if (m_CachedHappiness > kThisIsNotMyHappyPlaceHappiness && component3.m_AverageHappiness <= kThisIsNotMyHappyPlaceHappiness)
			{
				PlatformManager.instance.UnlockAchievement(Achievements.ThisIsNotMyHappyPlace);
			}
			m_CachedHappiness = component3.m_AverageHappiness;
			if (base.EntityManager.TryGetComponent<Tourism>(m_CitySystem.City, out var component4))
			{
				if (m_CachedAttractiveness < kSimplyIrresistibleAttractiveness && component4.m_Attractiveness >= kSimplyIrresistibleAttractiveness)
				{
					PlatformManager.instance.UnlockAchievement(Achievements.SimplyIrresistible);
				}
				m_CachedAttractiveness = component4.m_Attractiveness;
			}
		}
		if (!m_CreatedUniqueServiceBuildingQuery.IsEmptyIgnoreFilter && CheckOneOfEverything())
		{
			PlatformManager.instance.UnlockAchievement(Achievements.OneofEverything);
		}
		BufferLookup<CityStatistic> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef);
		value2 = m_CityStatisticsSystem.GetStatisticValue(bufferLookup, StatisticType.TouristCount) / 1000 * 1000;
		if (m_CachedTouristCount != value2)
		{
			PlatformManager.instance.IndicateAchievementProgress(Achievements.WelcomeOneandAll, value2);
		}
		m_CachedTouristCount = value2;
		int statisticValue = m_CityStatisticsSystem.GetStatisticValue(bufferLookup, StatisticType.EducationCount);
		int statisticValue2 = m_CityStatisticsSystem.GetStatisticValue(bufferLookup, StatisticType.EducationCount, 1);
		int statisticValue3 = m_CityStatisticsSystem.GetStatisticValue(bufferLookup, StatisticType.EducationCount, 2);
		int statisticValue4 = m_CityStatisticsSystem.GetStatisticValue(bufferLookup, StatisticType.EducationCount, 3);
		int statisticValue5 = m_CityStatisticsSystem.GetStatisticValue(bufferLookup, StatisticType.EducationCount, 4);
		int num4 = statisticValue + statisticValue2 + statisticValue3 + statisticValue4 + statisticValue5;
		if (num4 > 0 && (float)statisticValue5 / (float)num4 >= 0.15f)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.TopoftheClass);
		}
		if (!m_ServiceDistrictBuildingQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray3 = m_ServiceDistrictBuildingQuery.ToEntityArray(Allocator.TempJob);
			try
			{
				for (int l = 0; l < nativeArray3.Length; l++)
				{
					if (base.EntityManager.TryGetBuffer(nativeArray3[l], isReadOnly: true, out DynamicBuffer<ServiceDistrict> buffer2) && buffer2.Length > 0)
					{
						PlatformManager.instance.UnlockAchievement(Achievements.HappytobeofService);
					}
				}
			}
			finally
			{
				nativeArray3.Dispose();
			}
		}
		int num5 = CalculateEnergyProduction(m_RenewableEnergyProducersQuery);
		int num6 = CalculateEnergyProduction(m_FossilEnergyProducersQuery);
		if (num5 >= kZeroEmissionMinProduction && num6 <= 0)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.ZeroEmission);
		}
		int num7 = m_ResidentialBuildingsQuery.CalculateEntityCount();
		int num8 = m_CommercialBuildingsQuery.CalculateEntityCount();
		bool flag = false;
		bool flag2 = false;
		NativeArray<Entity> nativeArray4 = m_IndustrialBuildingsQuery.ToEntityArray(Allocator.TempJob);
		ComponentLookup<PrefabRef> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<OfficeBuilding> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref base.CheckedStateRef);
		try
		{
			for (int m = 0; m < nativeArray4.Length; m++)
			{
				if (componentLookup2.HasComponent(componentLookup[nativeArray4[m]].m_Prefab))
				{
					flag2 = true;
				}
				else
				{
					flag = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
		}
		finally
		{
			nativeArray4.Dispose();
		}
		if (flag && flag2 && num7 > 0 && num8 > 0)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.StrengthThroughDiversity);
		}
		if (!m_FollowedCitizensQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray5 = m_FollowedCitizensQuery.ToEntityArray(Allocator.TempJob);
			ComponentLookup<Citizen> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
			ComponentLookup<Followed> componentLookup4 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Followed_RO_ComponentLookup, ref base.CheckedStateRef);
			try
			{
				for (int n = 0; n < nativeArray5.Length; n++)
				{
					if (componentLookup3.HasComponent(nativeArray5[n]) && componentLookup3[nativeArray5[n]].GetAge() == CitizenAge.Elderly && componentLookup4.HasComponent(nativeArray5[n]) && componentLookup4[nativeArray5[n]].m_StartedFollowingAsChild)
					{
						PlatformManager.instance.UnlockAchievement(Achievements.YouLittleStalker);
					}
				}
			}
			finally
			{
				nativeArray5.Dispose();
			}
		}
		if (!m_PolicyModificationQuery.IsEmptyIgnoreFilter)
		{
			CheckPolicyAchievements();
		}
		if (m_PatientsTreatedCounter.Count > m_CachedPatientsTreatedCount)
		{
			int progress = m_PatientsTreatedCounter.Count - m_CachedPatientsTreatedCount;
			m_LittleBitOfTLCBuffer.AddProgress(progress);
			m_CachedPatientsTreatedCount = m_PatientsTreatedCounter.Count;
		}
		if (ShouldCheckOffshoreOilProduce() && m_OffshoreOilProduceCounter.Count > 0)
		{
			m_ADifferentPlatformerBuffer.AddProgress(m_OffshoreOilProduceCounter.Count);
			m_OffshoreOilProduceCounter.Count = 0;
		}
		if (ShouldCheckProducedFish() && m_ProducedFishCounter.Count > 0)
		{
			m_HowMuchIsTheFishBuffer.AddProgress(m_ProducedFishCounter.Count);
			m_ProducedFishCounter.Count = 0;
		}
		if (!m_CreatedUniqueBuildingQuery.IsEmptyIgnoreFilter)
		{
			int value3 = CountSignatureBuildings();
			PlatformManager.instance.IndicateAchievementProgress(Achievements.MakingAMark, value3);
			PlatformManager.instance.IndicateAchievementProgress(Achievements.TheArchitect, value3);
		}
		if (!m_ResidentialBuildingsQuery.IsEmptyIgnoreFilter && !m_CommercialBuildingsQuery.IsEmptyIgnoreFilter && !m_IndustrialBuildingsQuery.IsEmptyIgnoreFilter && !m_EnergyProducersQuery.IsEmptyIgnoreFilter && !m_WaterPumpingStationQuery.IsEmptyIgnoreFilter)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.MyFirstCity);
		}
		if (!m_CreatedPlantQuery.IsEmptyIgnoreFilter && m_PlantQuery.CalculateEntityCount() >= kColossalGardenerLimit)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.ColossalGardener);
		}
		if (CheckFourSeasons())
		{
			PlatformManager.instance.UnlockAchievement(Achievements.FourSeasons);
		}
		if (!m_CreatedAggregateElementQuery.IsEmptyIgnoreFilter && PlatformManager.instance.GetAchievement(Achievements.DrawMeLikeOneOfYourLiftBridges, out var achievement3) && !achievement3.achieved)
		{
			int value4 = CountLiftBridge();
			PlatformManager.instance.IndicateAchievementProgress(Achievements.DrawMeLikeOneOfYourLiftBridges, value4);
		}
		if (!m_TransportedResourceQueue.IsEmpty())
		{
			if (PlatformManager.instance.GetAchievement(Achievements.ShipIt, out var achievement4) && !achievement4.achieved && m_TransportWriteDeps.IsCompleted)
			{
				CheckTransportedResources();
			}
			else
			{
				m_TransportedResourceQueue.Clear();
			}
		}
		m_LastCheckFrameIndex = m_SimulationSystem.frameIndex;
	}

	private void CheckTransportedResources()
	{
		int num = 0;
		TransportedResource item;
		while (m_TransportedResourceQueue.TryDequeue(out item))
		{
			BufferLookup<AchievementFilterData> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AchievementFilterData_RO_BufferLookup, ref base.CheckedStateRef);
			if (base.EntityManager.TryGetComponent<PrefabRef>(item.m_CargoTransport, out var component) && bufferLookup.TryGetBuffer(component.m_Prefab, out var bufferData) && CheckFilter(bufferData, Achievements.ShipIt))
			{
				num += item.m_Amount;
			}
		}
		if (num > 0)
		{
			m_ShipItBuffer.AddProgress(num);
		}
	}

	private bool ShouldCheckOffshoreOilProduce()
	{
		if (m_ProduceResourceCompaniesQuery.IsEmptyIgnoreFilter)
		{
			return false;
		}
		if (PlatformManager.instance.GetAchievement(Achievements.ADifferentPlatformer, out var achievement) && !achievement.achieved)
		{
			return true;
		}
		return false;
	}

	private bool ShouldCheckProducedFish()
	{
		if (m_ProduceResourceCompaniesQuery.IsEmptyIgnoreFilter)
		{
			return false;
		}
		if (PlatformManager.instance.GetAchievement(Achievements.HowMuchIsTheFish, out var achievement) && !achievement.achieved)
		{
			return true;
		}
		return false;
	}

	private bool CheckFourSeasons()
	{
		if (!PlatformManager.instance.GetAchievement(Achievements.FourSeasons, out var achievement) || achievement.achieved)
		{
			return false;
		}
		Entity currentClimate = m_ClimateSystem.currentClimate;
		if (currentClimate == Entity.Null)
		{
			return false;
		}
		ClimatePrefab prefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(currentClimate);
		if (prefab == null)
		{
			return false;
		}
		ClimateSystem.SeasonInfo[] seasons = prefab.m_Seasons;
		if ((seasons != null && seasons.Length < 4) || prefab.temperatureRange.min > 0f)
		{
			return false;
		}
		TimeData singleton = m_TimeDataQuery.GetSingleton<TimeData>();
		TimeSettingsData singleton2 = m_TimeSettingsQuery.GetSingleton<TimeSettingsData>();
		float startingDate = m_TimeSystem.GetStartingDate(singleton2, singleton);
		float elapsedYears = m_TimeSystem.GetElapsedYears(singleton2, singleton);
		return prefab.CountElapsedSeasons(startingDate, elapsedYears) >= prefab.m_Seasons?.Length;
	}

	private int CalculateEnergyProduction(EntityQuery entityQuery)
	{
		int num = 0;
		NativeArray<ElectricityProducer> nativeArray = entityQuery.ToComponentDataArray<ElectricityProducer>(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			num += nativeArray[i].m_Capacity;
		}
		nativeArray.Dispose();
		return num;
	}

	private int CountLiftBridge()
	{
		NativeArray<Entity> nativeArray = m_AggregateElementQuery.ToEntityArray(Allocator.TempJob);
		int num = 0;
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (base.EntityManager.TryGetBuffer(nativeArray[i], isReadOnly: true, out DynamicBuffer<AggregateElement> buffer) && buffer.Length > 0 && !base.EntityManager.HasComponent<Owner>(buffer[0].m_Edge) && base.EntityManager.HasComponent<Road>(buffer[0].m_Edge) && base.EntityManager.TryGetComponent<PrefabRef>(buffer[0].m_Edge, out var component) && base.EntityManager.TryGetComponent<NetGeometryData>(component.m_Prefab, out var component2) && (component2.m_Flags & Game.Net.GeometryFlags.StraightEdges) == 0 && (component2.m_IntersectLayers & Layer.Waterway) != Layer.None)
			{
				num++;
			}
		}
		nativeArray.Dispose();
		return num;
	}

	private int CountSignatureBuildings()
	{
		NativeArray<PrefabRef> nativeArray = m_UniqueBuildingQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
		ComponentLookup<SignatureBuildingData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		int num = 0;
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (componentLookup.HasComponent(nativeArray[i].m_Prefab))
			{
				num++;
			}
		}
		nativeArray.Dispose();
		return num;
	}

	private int CountParks()
	{
		int num = 0;
		NativeArray<PrefabRef> nativeArray = m_ParkQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
		BufferLookup<AchievementFilterData> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AchievementFilterData_RO_BufferLookup, ref base.CheckedStateRef);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity prefab = nativeArray[i].m_Prefab;
			if (!bufferLookup.TryGetBuffer(prefab, out var bufferData) || CheckFilter(bufferData, Achievements.Groundskeeper, defaultResult: true))
			{
				num++;
			}
		}
		nativeArray.Dispose();
		return num;
	}

	private bool CheckOneOfEverything()
	{
		int num = CountUniqueServiceBuildingPrefabs();
		return CountUniqueServiceBuildings() == num;
	}

	private int CountUniqueServiceBuildingPrefabs()
	{
		BufferLookup<AchievementFilterData> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AchievementFilterData_RO_BufferLookup, ref base.CheckedStateRef);
		int num = 0;
		NativeArray<Entity> nativeArray = m_UniqueServiceBuildingPrefabQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (!bufferLookup.TryGetBuffer(nativeArray[i], out var bufferData) || CheckFilter(bufferData, Achievements.OneofEverything, defaultResult: true))
			{
				num++;
			}
		}
		nativeArray.Dispose();
		return num;
	}

	private int CountUniqueServiceBuildings()
	{
		BufferLookup<AchievementFilterData> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AchievementFilterData_RO_BufferLookup, ref base.CheckedStateRef);
		int num = 0;
		NativeArray<PrefabRef> nativeArray = m_UniqueServiceBuildingQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity prefab = nativeArray[i].m_Prefab;
			if (!bufferLookup.TryGetBuffer(prefab, out var bufferData) || CheckFilter(bufferData, Achievements.OneofEverything, defaultResult: true))
			{
				num++;
			}
		}
		nativeArray.Dispose();
		return num;
	}

	private bool CheckFilter(DynamicBuffer<AchievementFilterData> datas, AchievementId achievementID, bool defaultResult = false)
	{
		for (int i = 0; i < datas.Length; i++)
		{
			if (datas[i].m_AchievementID == achievementID)
			{
				return datas[i].m_Allow;
			}
		}
		return defaultResult;
	}

	private void CheckUnlockingAchievements()
	{
		if (!m_ServiceQuery.IsEmptyIgnoreFilter && m_LockedServiceQuery.IsEmpty)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.RoyalFlush);
		}
		if (!m_BuildingQuery.IsEmptyIgnoreFilter && m_LockedBuildingQuery.IsEmpty)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.KeyToTheCity);
		}
	}

	private void OnInfoviewChanged(InfoviewPrefab infoview, InfoviewPrefab oldInfoview)
	{
		m_ViewedInfoviews.Add(infoview);
		if (m_ViewedInfoviews.Count == m_InfoviewQuery.CalculateEntityCount())
		{
			PlatformManager.instance.UnlockAchievement(Achievements.TheInspector);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_LittleBitOfTLCBuffer.m_Progress;
		writer.Write(value);
		int value2 = m_HowMuchIsTheFishBuffer.m_Progress;
		writer.Write(value2);
		int value3 = m_ADifferentPlatformerBuffer.m_Progress;
		writer.Write(value3);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.TLCAchievement)
		{
			ref int value = ref m_LittleBitOfTLCBuffer.m_Progress;
			reader.Read(out value);
		}
		if (reader.context.format.Has(FormatTags.BPDLCAchievement))
		{
			ref int value2 = ref m_HowMuchIsTheFishBuffer.m_Progress;
			reader.Read(out value2);
			ref int value3 = ref m_ADifferentPlatformerBuffer.m_Progress;
			reader.Read(out value3);
		}
		else
		{
			m_HowMuchIsTheFishBuffer.m_Progress = 0;
			m_ADifferentPlatformerBuffer.m_Progress = 0;
		}
	}

	public void SetDefaults(Context context)
	{
		Reset();
		m_LittleBitOfTLCBuffer.m_Progress = 0;
		m_HowMuchIsTheFishBuffer.m_Progress = 0;
		m_ADifferentPlatformerBuffer.m_Progress = 0;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_PatientsTreatedCounter.Dispose();
		m_ProducedFishCounter.Dispose();
		m_OffshoreOilProduceCounter.Dispose();
		m_SquasherDownerBuffer.Dispose();
		m_ShipItBuffer.Dispose();
		m_TransportedResourceQueue.Dispose();
	}

	private void CheckPolicyAchievements()
	{
		if (m_DistrictQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		int num = 0;
		NativeArray<Entity> nativeArray = m_DistrictQuery.ToEntityArray(Allocator.TempJob);
		BufferLookup<Policy> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Policies_Policy_RO_BufferLookup, ref base.CheckedStateRef);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			DynamicBuffer<Policy> dynamicBuffer = bufferLookup[nativeArray[i]];
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				if ((dynamicBuffer[j].m_Flags & PolicyFlags.Active) != 0)
				{
					num++;
					break;
				}
			}
		}
		nativeArray.Dispose();
		if (num > 0)
		{
			PlatformManager.instance.UnlockAchievement(Achievements.ExecutiveDecision);
		}
		PlatformManager.instance.IndicateAchievementProgress(Achievements.WideVariety, num);
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
	public AchievementTriggerSystem()
	{
	}
}
