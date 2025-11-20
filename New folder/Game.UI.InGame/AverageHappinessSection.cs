using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class AverageHappinessSection : InfoSectionBase
{
	public enum Result
	{
		Visible,
		ResidentCount,
		Happiness,
		ResultCount
	}

	[BurstCompile]
	private struct CountHappinessJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<ResidentialProperty> m_ResidentialPropertyFromEntity;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdFromEntity;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterFromEntity;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenFromEntity;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerFromEntity;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerFromEntity;

		[ReadOnly]
		public ComponentLookup<Locked> m_LockedFromEntity;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformFromEntity;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducersFromEntity;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> m_CrimeProducersFromEntity;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducerFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDataFromEntity;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifierFromEntity;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverageFromEntity;

		[ReadOnly]
		public LocalEffectSystem.ReadData m_LocalEffectData;

		public CitizenHappinessParameterData m_CitizenHappinessParameters;

		public GarbageParameterData m_GarbageParameters;

		public HealthcareParameterData m_HealthcareParameters;

		public ParkParameterData m_ParkParameters;

		public EducationParameterData m_EducationParameters;

		public TelecomParameterData m_TelecomParameters;

		[ReadOnly]
		public DynamicBuffer<HappinessFactorParameterData> m_HappinessFactorParameters;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverage;

		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public NativeArray<int2> m_Factors;

		public NativeArray<int> m_Results;

		public Entity m_City;

		public float m_RelativeElectricityFee;

		public float m_RelativeWaterFee;

		public void Execute()
		{
			int happiness = 0;
			int citizenCount = 0;
			if (m_BuildingFromEntity.HasComponent(m_SelectedEntity) && m_ResidentialPropertyFromEntity.HasComponent(m_SelectedEntity))
			{
				BuildingHappiness.GetResidentialBuildingHappinessFactors(m_City, m_TaxRates, m_SelectedEntity, m_Factors, ref m_PrefabRefFromEntity, ref m_SpawnableBuildingDataFromEntity, ref m_BuildingPropertyDataFromEntity, ref m_CityModifierFromEntity, ref m_BuildingFromEntity, ref m_ElectricityConsumerFromEntity, ref m_WaterConsumerFromEntity, ref m_ServiceCoverageFromEntity, ref m_LockedFromEntity, ref m_TransformFromEntity, ref m_GarbageProducersFromEntity, ref m_CrimeProducersFromEntity, ref m_MailProducerFromEntity, ref m_RenterFromEntity, ref m_CitizenFromEntity, ref m_HouseholdCitizenFromEntity, ref m_BuildingDataFromEntity, ref m_LocalEffectData, m_CitizenHappinessParameters, m_GarbageParameters, m_HealthcareParameters, m_ParkParameters, m_EducationParameters, m_TelecomParameters, m_HappinessFactorParameters, m_PollutionMap, m_NoisePollutionMap, m_AirPollutionMap, m_TelecomCoverage, m_RelativeElectricityFee, m_RelativeWaterFee);
				if (TryAddPropertyHappiness(ref happiness, ref citizenCount, m_SelectedEntity, m_HouseholdFromEntity, m_CitizenFromEntity, m_HealthProblemFromEntity, m_RenterFromEntity, m_HouseholdCitizenFromEntity))
				{
					m_Results[1] = citizenCount;
					m_Results[2] = happiness;
				}
				m_Results[0] = ((citizenCount > 0) ? 1 : 0);
			}
			else
			{
				if (!m_HouseholdCitizenFromEntity.TryGetBuffer(m_SelectedEntity, out var bufferData))
				{
					return;
				}
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity citizen = bufferData[i].m_Citizen;
					if (m_CitizenFromEntity.HasComponent(citizen) && !CitizenUtils.IsDead(citizen, ref m_HealthProblemFromEntity))
					{
						happiness += m_CitizenFromEntity[citizen].Happiness;
						citizenCount++;
					}
				}
				m_Results[0] = 1;
				m_Results[1] = citizenCount;
				m_Results[2] = happiness;
				if (m_PropertyRenterFromEntity.TryGetComponent(m_SelectedEntity, out var componentData))
				{
					BuildingHappiness.GetResidentialBuildingHappinessFactors(m_City, m_TaxRates, componentData.m_Property, m_Factors, ref m_PrefabRefFromEntity, ref m_SpawnableBuildingDataFromEntity, ref m_BuildingPropertyDataFromEntity, ref m_CityModifierFromEntity, ref m_BuildingFromEntity, ref m_ElectricityConsumerFromEntity, ref m_WaterConsumerFromEntity, ref m_ServiceCoverageFromEntity, ref m_LockedFromEntity, ref m_TransformFromEntity, ref m_GarbageProducersFromEntity, ref m_CrimeProducersFromEntity, ref m_MailProducerFromEntity, ref m_RenterFromEntity, ref m_CitizenFromEntity, ref m_HouseholdCitizenFromEntity, ref m_BuildingDataFromEntity, ref m_LocalEffectData, m_CitizenHappinessParameters, m_GarbageParameters, m_HealthcareParameters, m_ParkParameters, m_EducationParameters, m_TelecomParameters, m_HappinessFactorParameters, m_PollutionMap, m_NoisePollutionMap, m_AirPollutionMap, m_TelecomCoverage, m_RelativeElectricityFee, m_RelativeWaterFee);
				}
			}
		}
	}

	[BurstCompile]
	public struct CountDistrictHappinessJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdFromEntity;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenFromEntity;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerFromEntity;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerFromEntity;

		[ReadOnly]
		public ComponentLookup<Locked> m_LockedFromEntity;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformFromEntity;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducersFromEntity;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> m_CrimeProducersFromEntity;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducerFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDataFromEntity;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifierFromEntity;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverageFromEntity;

		[ReadOnly]
		public LocalEffectSystem.ReadData m_LocalEffectData;

		public CitizenHappinessParameterData m_CitizenHappinessParameters;

		public GarbageParameterData m_GarbageParameters;

		public HealthcareParameterData m_HealthcareParameters;

		public ParkParameterData m_ParkParameters;

		public EducationParameterData m_EducationParameters;

		public TelecomParameterData m_TelecomParameters;

		[ReadOnly]
		public DynamicBuffer<HappinessFactorParameterData> m_HappinessFactorParameters;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverage;

		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public NativeArray<int2> m_Factors;

		public NativeArray<int> m_Results;

		public Entity m_City;

		public float m_RelativeElectricityFee;

		public float m_RelativeWaterFee;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			NativeArray<CurrentDistrict> nativeArray2 = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
			int num = 0;
			int happiness = 0;
			int citizenCount = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (!(nativeArray2[i].m_District != m_SelectedEntity) && m_SpawnableBuildingDataFromEntity.HasComponent(m_PrefabRefFromEntity[entity].m_Prefab) && TryAddPropertyHappiness(ref happiness, ref citizenCount, entity, m_HouseholdFromEntity, m_CitizenFromEntity, m_HealthProblemFromEntity, m_RenterFromEntity, m_HouseholdCitizenFromEntity))
				{
					num = 1;
					BuildingHappiness.GetResidentialBuildingHappinessFactors(m_City, m_TaxRates, entity, m_Factors, ref m_PrefabRefFromEntity, ref m_SpawnableBuildingDataFromEntity, ref m_BuildingPropertyDataFromEntity, ref m_CityModifierFromEntity, ref m_BuildingFromEntity, ref m_ElectricityConsumerFromEntity, ref m_WaterConsumerFromEntity, ref m_ServiceCoverageFromEntity, ref m_LockedFromEntity, ref m_TransformFromEntity, ref m_GarbageProducersFromEntity, ref m_CrimeProducersFromEntity, ref m_MailProducerFromEntity, ref m_RenterFromEntity, ref m_CitizenFromEntity, ref m_HouseholdCitizenFromEntity, ref m_BuildingDataFromEntity, ref m_LocalEffectData, m_CitizenHappinessParameters, m_GarbageParameters, m_HealthcareParameters, m_ParkParameters, m_EducationParameters, m_TelecomParameters, m_HappinessFactorParameters, m_PollutionMap, m_NoisePollutionMap, m_AirPollutionMap, m_TelecomCoverage, m_RelativeElectricityFee, m_RelativeWaterFee);
				}
			}
			m_Results[0] += num;
			m_Results[1] += citizenCount;
			m_Results[2] += happiness;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public BufferLookup<HappinessFactorParameterData> __Game_Prefabs_HappinessFactorParameterData_RW_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResidentialProperty> __Game_Buildings_ResidentialProperty_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_HappinessFactorParameterData_RW_BufferLookup = state.GetBufferLookup<HappinessFactorParameterData>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Buildings_ResidentialProperty_RO_ComponentLookup = state.GetComponentLookup<ResidentialProperty>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		}
	}

	private GroundPollutionSystem m_GroundPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private TaxSystem m_TaxSystem;

	private CitySystem m_CitySystem;

	private LocalEffectSystem m_LocalEffectSystem;

	private EntityQuery m_DistrictBuildingQuery;

	public NativeArray<int> m_Results;

	private NativeArray<int2> m_Factors;

	protected EntityQuery m_CitizenHappinessParameterQuery;

	protected EntityQuery m_GarbageParameterQuery;

	protected EntityQuery m_HappinessFactorParameterQuery;

	protected EntityQuery m_HealthcareParameterQuery;

	protected EntityQuery m_ParkParameterQuery;

	protected EntityQuery m_EducationParameterQuery;

	protected EntityQuery m_TelecomParameterQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1244325462_0;

	protected override string group => "AverageHappinessSection";

	private CitizenHappiness averageHappiness { get; set; }

	private NativeList<FactorInfo> happinessFactors { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_DistrictBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Renter>(), ComponentType.ReadOnly<CurrentDistrict>(), ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_GarbageParameterQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
		m_HappinessFactorParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HappinessFactorParameterData>());
		m_HealthcareParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_ParkParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ParkParameterData>());
		m_EducationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
		m_TelecomParameterQuery = GetEntityQuery(ComponentType.ReadOnly<TelecomParameterData>());
		m_Factors = new NativeArray<int2>(29, Allocator.Persistent);
		happinessFactors = new NativeList<FactorInfo>(10, Allocator.Persistent);
		m_Results = new NativeArray<int>(3, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		m_Factors.Dispose();
		happinessFactors.Dispose();
		base.OnDestroy();
	}

	protected override void Reset()
	{
		for (int i = 0; i < m_Factors.Length; i++)
		{
			m_Factors[i] = 0;
		}
		averageHappiness = default(CitizenHappiness);
		happinessFactors.Clear();
		m_Results[0] = 0;
		m_Results[1] = 0;
		m_Results[2] = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CitizenHappinessParameterData singleton = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>();
		GarbageParameterData singleton2 = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>();
		DynamicBuffer<HappinessFactorParameterData> bufferAfterCompletingDependency = InternalCompilerInterface.GetBufferAfterCompletingDependency(ref __TypeHandle.__Game_Prefabs_HappinessFactorParameterData_RW_BufferLookup, ref base.CheckedStateRef, m_HappinessFactorParameterQuery.GetSingletonEntity());
		HealthcareParameterData singleton3 = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>();
		ParkParameterData singleton4 = m_ParkParameterQuery.GetSingleton<ParkParameterData>();
		EducationParameterData singleton5 = m_EducationParameterQuery.GetSingleton<EducationParameterData>();
		TelecomParameterData singleton6 = m_TelecomParameterQuery.GetSingleton<TelecomParameterData>();
		ServiceFeeParameterData singleton7 = __query_1244325462_0.GetSingleton<ServiceFeeParameterData>();
		JobHandle dependencies;
		NativeArray<GroundPollution> buffer = m_GroundPollutionSystem.GetData(readOnly: true, out dependencies).m_Buffer;
		JobHandle dependencies2;
		NativeArray<NoisePollution> buffer2 = m_NoisePollutionSystem.GetData(readOnly: true, out dependencies2).m_Buffer;
		JobHandle dependencies3;
		NativeArray<AirPollution> buffer3 = m_AirPollutionSystem.GetData(readOnly: true, out dependencies3).m_Buffer;
		JobHandle dependencies4;
		CellMapData<TelecomCoverage> data = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies4);
		dependencies.Complete();
		dependencies2.Complete();
		dependencies3.Complete();
		dependencies4.Complete();
		NativeArray<int> taxRates = m_TaxSystem.GetTaxRates();
		DynamicBuffer<ServiceFee> buffer4 = base.EntityManager.GetBuffer<ServiceFee>(m_CitySystem.City);
		float relativeElectricityFee = ServiceFeeSystem.GetFee(PlayerResource.Electricity, buffer4) / singleton7.m_ElectricityFee.m_Default;
		float relativeWaterFee = ServiceFeeSystem.GetFee(PlayerResource.Water, buffer4) / singleton7.m_WaterFee.m_Default;
		if (base.EntityManager.HasComponent<District>(selectedEntity) && base.EntityManager.HasComponent<Area>(selectedEntity))
		{
			JobChunkExtensions.Schedule(new CountDistrictHappinessJob
			{
				m_SelectedEntity = selectedEntity,
				m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CitizenFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentDistrictHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HealthProblemFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizenFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_RenterFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingPropertyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConsumerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterConsumerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LockedFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GarbageProducersFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CrimeProducersFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MailProducerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CityModifierFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_ServiceCoverageFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
				m_LocalEffectData = m_LocalEffectSystem.GetReadData(out var dependencies5),
				m_CitizenHappinessParameters = singleton,
				m_GarbageParameters = singleton2,
				m_HealthcareParameters = singleton3,
				m_ParkParameters = singleton4,
				m_EducationParameters = singleton5,
				m_TelecomParameters = singleton6,
				m_HappinessFactorParameters = bufferAfterCompletingDependency,
				m_TelecomCoverage = data,
				m_PollutionMap = buffer,
				m_NoisePollutionMap = buffer2,
				m_AirPollutionMap = buffer3,
				m_TaxRates = taxRates,
				m_Factors = m_Factors,
				m_Results = m_Results,
				m_City = m_CitySystem.City,
				m_RelativeElectricityFee = relativeElectricityFee,
				m_RelativeWaterFee = relativeWaterFee
			}, m_DistrictBuildingQuery, JobHandle.CombineDependencies(base.Dependency, dependencies5)).Complete();
			base.visible = m_Results[0] > 0;
		}
		else
		{
			IJobExtensions.Schedule(new CountHappinessJob
			{
				m_SelectedEntity = selectedEntity,
				m_BuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResidentialPropertyFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ResidentialProperty_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HealthProblemFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenterFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CitizenFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizenFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_RenterFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingPropertyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConsumerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaterConsumerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LockedFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GarbageProducersFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CrimeProducersFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MailProducerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CityModifierFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_ServiceCoverageFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
				m_LocalEffectData = m_LocalEffectSystem.GetReadData(out var dependencies6),
				m_CitizenHappinessParameters = singleton,
				m_GarbageParameters = singleton2,
				m_HealthcareParameters = singleton3,
				m_ParkParameters = singleton4,
				m_EducationParameters = singleton5,
				m_TelecomParameters = singleton6,
				m_HappinessFactorParameters = bufferAfterCompletingDependency,
				m_TelecomCoverage = data,
				m_PollutionMap = buffer,
				m_NoisePollutionMap = buffer2,
				m_AirPollutionMap = buffer3,
				m_TaxRates = taxRates,
				m_Factors = m_Factors,
				m_Results = m_Results,
				m_City = m_CitySystem.City,
				m_RelativeElectricityFee = relativeElectricityFee,
				m_RelativeWaterFee = relativeWaterFee
			}, JobHandle.CombineDependencies(base.Dependency, dependencies6)).Complete();
			base.visible = m_Results[0] > 0;
		}
	}

	protected override void OnProcess()
	{
		int num = m_Results[1];
		int num2 = m_Results[2];
		averageHappiness = CitizenUIUtils.GetCitizenHappiness(num2 / math.select(num, 1, num == 0));
		for (int i = 0; i < m_Factors.Length; i++)
		{
			int x = m_Factors[i].x;
			if (x > 0)
			{
				float num3 = math.round((float)m_Factors[i].y / (float)x);
				if (num3 != 0f)
				{
					happinessFactors.Add(new FactorInfo(i, (int)num3));
				}
			}
		}
		happinessFactors.Sort();
		if (base.EntityManager.HasComponent<Building>(selectedEntity))
		{
			base.tooltipKeys.Add("Building");
		}
		else if (base.EntityManager.HasComponent<Household>(selectedEntity))
		{
			base.tooltipKeys.Add("Household");
		}
		else
		{
			base.tooltipKeys.Add("District");
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("averageHappiness");
		writer.Write(averageHappiness);
		int num = math.min(10, happinessFactors.Length);
		writer.PropertyName("happinessFactors");
		writer.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			happinessFactors[i].WriteBuildingHappinessFactor(writer);
		}
		writer.ArrayEnd();
	}

	private static bool TryAddPropertyHappiness(ref int happiness, ref int citizenCount, Entity entity, ComponentLookup<Household> householdFromEntity, ComponentLookup<Citizen> citizenFromEntity, ComponentLookup<HealthProblem> healthProblemFromEntity, BufferLookup<Renter> renterFromEntity, BufferLookup<HouseholdCitizen> householdCitizenFromEntity)
	{
		bool result = false;
		if (renterFromEntity.TryGetBuffer(entity, out var bufferData))
		{
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity renter = bufferData[i].m_Renter;
				if (!householdFromEntity.HasComponent(renter) || !householdCitizenFromEntity.TryGetBuffer(renter, out var bufferData2))
				{
					continue;
				}
				result = true;
				for (int j = 0; j < bufferData2.Length; j++)
				{
					Entity citizen = bufferData2[j].m_Citizen;
					if (citizenFromEntity.HasComponent(citizen) && !CitizenUtils.IsDead(citizen, ref healthProblemFromEntity))
					{
						happiness += citizenFromEntity[citizen].Happiness;
						citizenCount++;
					}
				}
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1244325462_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public AverageHappinessSection()
	{
	}
}
