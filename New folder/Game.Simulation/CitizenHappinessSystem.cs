using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
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
public class CitizenHappinessSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public enum HappinessFactor
	{
		Telecom,
		Crime,
		AirPollution,
		Apartment,
		Electricity,
		Healthcare,
		GroundPollution,
		NoisePollution,
		Water,
		WaterPollution,
		Sewage,
		Garbage,
		Entertainment,
		Education,
		Mail,
		Welfare,
		Leisure,
		Tax,
		Buildings,
		Consumption,
		TrafficPenalty,
		DeathPenalty,
		Homelessness,
		ElectricityFee,
		WaterFee,
		Unemployment,
		Count
	}

	private struct FactorItem
	{
		public HappinessFactor m_Factor;

		public int4 m_Value;

		public uint m_UpdateFrame;
	}

	[BurstCompile]
	private struct CitizenHappinessJob : IJobChunk
	{
		[NativeDisableContainerSafetyRestriction]
		public NativeQueue<int>.ParallelWriter m_DebugQueue;

		public bool m_DebugOn;

		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public ComponentTypeHandle<CrimeVictim> m_CrimeVictimType;

		[ReadOnly]
		public ComponentTypeHandle<Criminal> m_CriminalType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_Properties;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_Garbages;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Locked> m_Locked;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> m_CrimeProducers;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducers;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_ServiceFees;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Prison> m_Prisons;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.School> m_Schools;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<AirPollution> m_AirPollutionMap;

		[ReadOnly]
		public NativeArray<NoisePollution> m_NoisePollutionMap;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverage;

		[ReadOnly]
		public LocalEffectSystem.ReadData m_LocalEffectData;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public HealthcareParameterData m_HealthcareParameters;

		public ParkParameterData m_ParkParameters;

		public EducationParameterData m_EducationParameters;

		public TelecomParameterData m_TelecomParameters;

		public GarbageParameterData m_GarbageParameters;

		public PoliceConfigurationData m_PoliceParameters;

		public CitizenHappinessParameterData m_CitizenHappinessParameters;

		public LeisureParametersData m_LeisureParameters;

		public TimeSettingsData m_TimeSettings;

		public ServiceFeeParameterData m_FeeParameters;

		public TimeData m_TimeData;

		public Entity m_City;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public uint m_RawUpdateFrame;

		public NativeQueue<FactorItem>.ParallelWriter m_FactorQueue;

		public uint m_SimulationFrame;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		private void AddData(float value)
		{
			if (m_DebugOn)
			{
				m_DebugQueue.Enqueue(Mathf.RoundToInt(value));
			}
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Citizen> nativeArray2 = chunk.GetNativeArray(ref m_CitizenType);
			NativeArray<HouseholdMember> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdMemberType);
			NativeArray<CrimeVictim> nativeArray4 = chunk.GetNativeArray(ref m_CrimeVictimType);
			NativeArray<Criminal> nativeArray5 = chunk.GetNativeArray(ref m_CriminalType);
			NativeArray<Game.Citizens.Student> nativeArray6 = chunk.GetNativeArray(ref m_StudentType);
			NativeArray<CurrentBuilding> nativeArray7 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			NativeArray<HealthProblem> nativeArray8 = chunk.GetNativeArray(ref m_HealthProblemType);
			EnabledMask enabledMask = chunk.GetEnabledMask(ref m_CrimeVictimType);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			DynamicBuffer<ServiceFee> fees = m_ServiceFees[m_City];
			float relativeFee = ServiceFeeSystem.GetFee(PlayerResource.Electricity, fees) / m_FeeParameters.m_ElectricityFee.m_Default;
			float relativeFee2 = ServiceFeeSystem.GetFee(PlayerResource.Water, fees) / m_FeeParameters.m_WaterFee.m_Default;
			int4 value = default(int4);
			int4 value2 = default(int4);
			int4 value3 = default(int4);
			int4 value4 = default(int4);
			int4 value5 = default(int4);
			int4 value6 = default(int4);
			int4 value7 = default(int4);
			int4 value8 = default(int4);
			int4 value9 = default(int4);
			int4 value10 = default(int4);
			int4 value11 = default(int4);
			int4 value12 = default(int4);
			int4 value13 = default(int4);
			int4 value14 = default(int4);
			int4 value15 = default(int4);
			int4 value16 = default(int4);
			int4 value17 = default(int4);
			int4 value18 = default(int4);
			int4 value19 = default(int4);
			int4 value20 = default(int4);
			int4 value21 = default(int4);
			int4 value22 = default(int4);
			int4 value23 = default(int4);
			int4 value24 = default(int4);
			int4 value25 = default(int4);
			int4 value26 = default(int4);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < chunk.Count; i++)
			{
				_ = nativeArray[i];
				Entity household = nativeArray3[i].m_Household;
				if (!m_Resources.HasBuffer(household))
				{
					return;
				}
				Citizen citizen = nativeArray2[i];
				if ((CollectionUtils.TryGet(nativeArray8, i, out var value27) && CitizenUtils.IsDead(value27)) || ((m_Households[household].m_Flags & HouseholdFlags.MovedIn) == 0 && (citizen.m_State & CitizenFlags.Tourist) == 0))
				{
					continue;
				}
				Entity entity = Entity.Null;
				Entity entity2 = Entity.Null;
				if (m_Properties.HasComponent(household))
				{
					entity = m_Properties[household].m_Property;
					if (m_CurrentDistrictData.HasComponent(entity))
					{
						entity2 = m_CurrentDistrictData[entity].m_District;
					}
				}
				DynamicBuffer<HouseholdCitizen> householdCitizens = m_HouseholdCitizens[household];
				int num3 = 0;
				for (int j = 0; j < householdCitizens.Length; j++)
				{
					if (citizen.GetAge() == CitizenAge.Child)
					{
						num3++;
					}
				}
				int shoppedValueLastDay = (int)m_Households[household].m_ShoppedValueLastDay;
				int2 @int = ((shoppedValueLastDay > 0) ? new int2(0, math.min(15, shoppedValueLastDay / 50)) : default(int2));
				value22.x += @int.x + @int.y;
				value22.y++;
				value22.z += @int.x;
				value22.w += @int.y;
				int2 int2 = 0;
				if (CollectionUtils.TryGet(nativeArray5, i, out var value28) && (value28.m_Flags & CriminalFlags.Prisoner) != 0 && CollectionUtils.TryGet(nativeArray7, i, out var value29) && m_Prisons.TryGetComponent(value29.m_CurrentBuilding, out var componentData))
				{
					int2 += new int2(componentData.m_PrisonerHealth, componentData.m_PrisonerWellbeing);
				}
				if (CollectionUtils.TryGet(nativeArray6, i, out var value30) && m_Schools.TryGetComponent(value30.m_School, out var componentData2))
				{
					int2 += new int2(componentData2.m_StudentHealth, componentData2.m_StudentWellbeing);
				}
				value21 += new int4(int2.x + int2.y, 1, int2.x, int2.y);
				int2 int3 = new int2(0, 0);
				int2 int4 = default(int2);
				int2 int5 = default(int2);
				int2 int6 = default(int2);
				int2 int7 = default(int2);
				int2 int8 = default(int2);
				int2 int9 = default(int2);
				int2 int10 = default(int2);
				int2 int11 = default(int2);
				int2 int12 = default(int2);
				int2 int13 = default(int2);
				int2 int14 = default(int2);
				int2 int15 = default(int2);
				int2 int16 = default(int2);
				int2 int17 = default(int2);
				int2 int18 = default(int2);
				int2 int19 = default(int2);
				int2 int20 = default(int2);
				int2 int21 = default(int2);
				int2 int22 = default(int2);
				int2 int23 = default(int2);
				CrimeVictim crimeVictim = default(CrimeVictim);
				if (enabledMask[i])
				{
					crimeVictim = nativeArray4[i];
				}
				if (m_Properties.TryGetComponent(household, out var componentData3) && m_Prefabs.TryGetComponent(componentData3.m_Property, out var componentData4))
				{
					Entity prefab = componentData4.m_Prefab;
					Entity property = componentData3.m_Property;
					Entity healthcareServicePrefab = m_HealthcareParameters.m_HealthcareServicePrefab;
					Entity parkServicePrefab = m_ParkParameters.m_ParkServicePrefab;
					Entity educationServicePrefab = m_EducationParameters.m_EducationServicePrefab;
					Entity telecomServicePrefab = m_TelecomParameters.m_TelecomServicePrefab;
					Entity garbageServicePrefab = m_GarbageParameters.m_GarbageServicePrefab;
					Entity policeServicePrefab = m_PoliceParameters.m_PoliceServicePrefab;
					Entity entity3 = Entity.Null;
					float curvePosition = 0f;
					if (m_Buildings.HasComponent(property))
					{
						Building building = m_Buildings[property];
						entity3 = building.m_RoadEdge;
						curvePosition = building.m_CurvePosition;
					}
					int4 = GetElectricitySupplyBonuses(property, ref m_ElectricityConsumers, in m_CitizenHappinessParameters);
					value5.x += int4.x + int4.y;
					value5.z += int4.x;
					value5.w += int4.y;
					value5.y++;
					int5 = GetElectricityFeeBonuses(property, ref m_ElectricityConsumers, relativeFee, in m_CitizenHappinessParameters);
					value6.x += int5.x + int5.y;
					value6.z += int5.x;
					value6.w += int5.y;
					value6.y++;
					int10 = GetWaterSupplyBonuses(property, ref m_WaterConsumers, in m_CitizenHappinessParameters);
					value10.x += int10.x + int10.y;
					value10.z += int10.x;
					value10.w += int10.y;
					value10.y++;
					int11 = GetWaterFeeBonuses(property, ref m_WaterConsumers, relativeFee2, in m_CitizenHappinessParameters);
					value11.x += int11.x + int11.y;
					value11.z += int11.x;
					value11.w += int11.y;
					value11.y++;
					int12 = GetWaterPollutionBonuses(property, ref m_WaterConsumers, cityModifiers, in m_CitizenHappinessParameters);
					value12.x += int12.x + int12.y;
					value12.z += int12.x;
					value12.w += int12.y;
					value12.y++;
					int13 = GetSewageBonuses(property, ref m_WaterConsumers, in m_CitizenHappinessParameters);
					value13.x += int13.x + int13.y;
					value13.z += int13.x;
					value13.w += int13.y;
					value13.y++;
					if (m_ServiceCoverages.HasBuffer(entity3))
					{
						DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage = m_ServiceCoverages[entity3];
						int6 = GetHealthcareBonuses(curvePosition, serviceCoverage, ref m_Locked, healthcareServicePrefab, in m_CitizenHappinessParameters);
						value7.x += int6.x + int6.y;
						value7.z += int6.x;
						value7.w += int6.y;
						value7.y++;
						int16 = GetEntertainmentBonuses(curvePosition, serviceCoverage, cityModifiers, ref m_Locked, parkServicePrefab, in m_CitizenHappinessParameters);
						value15.x += int16.x + int16.y;
						value15.z += int16.x;
						value15.w += int16.y;
						value15.y++;
						int17 = GetEducationBonuses(curvePosition, serviceCoverage, ref m_Locked, educationServicePrefab, in m_CitizenHappinessParameters, num3);
						value16.x += int17.x + int17.y;
						value16.z += int17.x;
						value16.w += int17.y;
						value16.y++;
						int20 = GetWellfareBonuses(curvePosition, serviceCoverage, in m_CitizenHappinessParameters, citizen.Happiness);
						value18.x += int20.x + int20.y;
						value18.z += int20.x;
						value18.w += int20.y;
						value18.y++;
					}
					int7 = GetGroundPollutionBonuses(property, ref m_Transforms, m_PollutionMap, cityModifiers, in m_CitizenHappinessParameters);
					value8.x += int7.x + int7.y;
					value8.z += int7.x;
					value8.w += int7.y;
					value8.y++;
					int8 = GetAirPollutionBonuses(property, ref m_Transforms, m_AirPollutionMap, cityModifiers, in m_CitizenHappinessParameters);
					value3.x += int8.x + int8.y;
					value3.z += int8.x;
					value3.w += int8.y;
					value3.y++;
					int9 = GetNoiseBonuses(property, ref m_Transforms, m_NoisePollutionMap, in m_CitizenHappinessParameters);
					value9.x += int9.x + int9.y;
					value9.z += int9.x;
					value9.w += int9.y;
					value9.y++;
					int14 = GetGarbageBonuses(property, ref m_Garbages, ref m_Locked, garbageServicePrefab, in m_GarbageParameters);
					value14.x += int14.x + int14.y;
					value14.z += int14.x;
					value14.w += int14.y;
					value14.y++;
					int15 = GetCrimeBonuses(crimeVictim, property, ref m_CrimeProducers, ref m_Locked, policeServicePrefab, in m_CitizenHappinessParameters);
					value.x += int15.x + int15.y;
					value.z += int15.x;
					value.w += int15.y;
					value.y++;
					int18 = GetMailBonuses(property, ref m_MailProducers, ref m_Locked, telecomServicePrefab, in m_CitizenHappinessParameters);
					value17.x += int18.x + int18.y;
					value17.z += int18.x;
					value17.w += int18.y;
					value17.y++;
					int19 = GetTelecomBonuses(property, ref m_Transforms, m_TelecomCoverage, ref m_Locked, telecomServicePrefab, in m_CitizenHappinessParameters);
					value2.x += int19.x + int19.y;
					value2.z += int19.x;
					value2.w += int19.y;
					value2.y++;
					int23 = GetUnemploymentBonuses(ref citizen, in m_CitizenHappinessParameters);
					value26.x += int23.x + int23.y;
					value26.z += int23.x;
					value26.w += int23.y;
					value26.y++;
					value25.y++;
					if (m_SpawnableBuildings.HasComponent(prefab) && m_BuildingDatas.HasComponent(prefab) && m_BuildingPropertyDatas.HasComponent(prefab) && !m_HomelessHouseholds.HasComponent(household))
					{
						SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildings[prefab];
						BuildingData buildingData = m_BuildingDatas[prefab];
						BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
						float num4 = buildingPropertyData.m_SpaceMultiplier * (float)buildingData.m_LotSize.x * (float)buildingData.m_LotSize.y / (float)(householdCitizens.Length * buildingPropertyData.m_ResidentialProperties);
						int3.y = Mathf.RoundToInt(GetApartmentWellbeing(num4, spawnableBuildingData.m_Level));
						value4.x += int3.x + int3.y;
						value4.z += int3.x;
						value4.w += int3.y;
						value4.y++;
						AddData(math.min(100f, 100f * num4));
					}
					else
					{
						int3.y = Mathf.RoundToInt(GetApartmentWellbeing(0.01f, 1));
						value4.x += int3.y;
						value4.w += int3.y;
						value4.y++;
						int22 = GetHomelessBonuses(in m_CitizenHappinessParameters);
						value25.x += int22.x + int22.y;
						value25.z += int22.x;
						value25.w += int22.y;
					}
				}
				bool flag = (citizen.m_State & CitizenFlags.Tourist) != 0;
				if (random.NextInt(0, 100) < (flag ? m_LeisureParameters.m_ChanceTouristDecreaseLeisureCounter : m_LeisureParameters.m_ChanceCitizenDecreaseLeisureCounter))
				{
					citizen.m_LeisureCounter = (byte)math.min(255, math.max(0, citizen.m_LeisureCounter - m_LeisureParameters.m_AmountLeisureCounterDecrease));
				}
				citizen.m_PenaltyCounter = (byte)math.max(0, citizen.m_PenaltyCounter - 1);
				int2 leisureBonuses = GetLeisureBonuses(citizen);
				value19.x += leisureBonuses.x + leisureBonuses.y;
				value19.z += leisureBonuses.x;
				value19.w += leisureBonuses.y;
				value19.y++;
				if (!flag)
				{
					int21 = GetTaxBonuses(citizen.GetEducationLevel(), m_TaxRates, cityModifiers, in m_CitizenHappinessParameters);
				}
				value20.x += int21.x + int21.y;
				value20.z += int21.x;
				value20.w += int21.y;
				value20.y++;
				int2 sicknessBonuses = GetSicknessBonuses(nativeArray8.Length != 0, ref citizen, in m_CitizenHappinessParameters);
				value7.x += sicknessBonuses.x + sicknessBonuses.y;
				value7.z += sicknessBonuses.x;
				value7.w += sicknessBonuses.y;
				value7.y++;
				int2 deathPenalty = GetDeathPenalty(householdCitizens, ref m_HealthProblems, in m_CitizenHappinessParameters);
				value24.x += deathPenalty.x + deathPenalty.y;
				value24.z += deathPenalty.x;
				value24.w += deathPenalty.y;
				value24.y++;
				int num5 = ((citizen.m_PenaltyCounter > 0) ? m_CitizenHappinessParameters.m_PenaltyEffect : 0);
				value23.x += num5;
				value23.w += num5;
				value23.y++;
				int num6 = math.max(0, 50 + num5 + deathPenalty.y + @int.y + int4.y + int5.y + int10.y + int11.y + int13.y + int6.y + leisureBonuses.y + int2.y + int12.y + int9.y + int14.y + int15.y + int16.y + int18.y + int17.y + int19.y + int3.y + int20.y + int21.y + int22.y + int23.y);
				int num7 = 50 + int6.x + sicknessBonuses.x + deathPenalty.x + int2.x + int7.x + int8.x + int4.x + int10.x + int13.x + int12.x + int14.x + int3.x + int20.x + int22.x + int23.x;
				num7 += math.select(0, 1, (citizen.m_State & CitizenFlags.BicycleUser) != 0);
				float wellbeing = num6;
				float health = num7;
				GetLocalEffectBonuses(ref wellbeing, ref health, ref m_LocalEffectData, ref m_Transforms, entity);
				if (m_DistrictModifiers.HasBuffer(entity2))
				{
					DynamicBuffer<DistrictModifier> modifiers = m_DistrictModifiers[entity2];
					AreaUtils.ApplyModifier(ref wellbeing, modifiers, DistrictModifierType.Wellbeing);
				}
				num6 = Mathf.RoundToInt(wellbeing);
				num7 = Mathf.RoundToInt(health);
				int num8 = ((random.NextInt(100) > 50 + citizen.m_WellBeing - num6) ? 1 : (-1));
				citizen.m_WellBeing = (byte)math.max(0, math.min(100, citizen.m_WellBeing + num8));
				num8 = ((random.NextInt(100) > 50 + citizen.m_Health - num7) ? 1 : (-1));
				int maxHealth = GetMaxHealth(citizen.GetAgeInDays(m_SimulationFrame, m_TimeData) / (float)m_TimeSettings.m_DaysPerYear);
				citizen.m_Health = (byte)math.max(0, math.min(maxHealth, citizen.m_Health + num8));
				if (citizen.m_WellBeing < m_CitizenHappinessParameters.m_LowWellbeing)
				{
					num++;
				}
				if (citizen.m_Health < m_CitizenHappinessParameters.m_LowHealth)
				{
					num2++;
				}
				nativeArray2[i] = citizen;
			}
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Telecom,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value2
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Crime,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.AirPollution,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value3
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Apartment,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value4
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Electricity,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value5
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.ElectricityFee,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value6
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Healthcare,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value7
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.GroundPollution,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value8
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.NoisePollution,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value9
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Water,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value10
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.WaterFee,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value11
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.WaterPollution,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value12
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Sewage,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value13
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Garbage,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value14
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Entertainment,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value15
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Education,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value16
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Mail,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value17
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Welfare,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value18
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Leisure,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value19
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Tax,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value20
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Buildings,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value21
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Consumption,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value22
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.TrafficPenalty,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value23
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.DeathPenalty,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value24
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Homelessness,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value25
			});
			m_FactorQueue.Enqueue(new FactorItem
			{
				m_Factor = HappinessFactor.Unemployment,
				m_UpdateFrame = m_RawUpdateFrame,
				m_Value = value26
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.WellbeingLevel,
				m_Change = num
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.HealthLevel,
				m_Change = num2
			});
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HappinessFactorJob : IJob
	{
		public NativeArray<int4> m_HappinessFactors;

		public NativeQueue<FactorItem> m_FactorQueue;

		public NativeQueue<TriggerAction> m_TriggerActionQueue;

		public uint m_RawUpdateFrame;

		public Entity m_ParameterEntity;

		[ReadOnly]
		public BufferLookup<HappinessFactorParameterData> m_Parameters;

		[ReadOnly]
		public ComponentLookup<Locked> m_Locked;

		public void Execute()
		{
			for (int i = 0; i < 26; i++)
			{
				m_HappinessFactors[GetFactorIndex((HappinessFactor)i, m_RawUpdateFrame)] = default(int4);
			}
			FactorItem item;
			while (m_FactorQueue.TryDequeue(out item))
			{
				if (item.m_UpdateFrame != m_RawUpdateFrame)
				{
					UnityEngine.Debug.LogWarning("Different updateframe in HappinessFactorJob than in its queue");
				}
				m_HappinessFactors[GetFactorIndex(item.m_Factor, item.m_UpdateFrame)] += item.m_Value;
			}
			DynamicBuffer<HappinessFactorParameterData> parameters = m_Parameters[m_ParameterEntity];
			for (int j = 0; j < 26; j++)
			{
				m_TriggerActionQueue.Enqueue(new TriggerAction(GetTriggerTypeForHappinessFactor((HappinessFactor)j), Entity.Null, GetHappinessFactor((HappinessFactor)j, m_HappinessFactors, parameters, ref m_Locked).x));
			}
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<Citizen> __Game_Citizens_Citizen_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CrimeVictim> __Game_Citizens_CrimeVictim_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Criminal> __Game_Citizens_Criminal_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Prison> __Game_Buildings_Prison_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.School> __Game_Buildings_School_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HappinessFactorParameterData> __Game_Prefabs_HappinessFactorParameterData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Citizen_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Citizen>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_CrimeVictim_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeVictim>(isReadOnly: true);
			__Game_Citizens_Criminal_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Criminal>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_Buildings_Prison_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Prison>(isReadOnly: true);
			__Game_Buildings_School_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.School>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Prefabs_HappinessFactorParameterData_RO_BufferLookup = state.GetBufferLookup<HappinessFactorParameterData>(isReadOnly: true);
		}
	}

	[DebugWatchValue]
	private DebugWatchDistribution m_DebugData;

	private NativeQueue<FactorItem> m_FactorQueue;

	private EntityQuery m_CitizenQuery;

	private EntityQuery m_HappinessFactorParameterQuery;

	private SimulationSystem m_SimulationSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private LocalEffectSystem m_LocalEffectSystem;

	private CitySystem m_CitySystem;

	private TriggerSystem m_TriggerSystem;

	private TaxSystem m_TaxSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_HealthcareParameterQuery;

	private EntityQuery m_ParkParameterQuery;

	private EntityQuery m_EducationParameterQuery;

	private EntityQuery m_TelecomParameterQuery;

	private EntityQuery m_GarbageParameterQuery;

	private EntityQuery m_PoliceParameterQuery;

	private EntityQuery m_CitizenHappinessParameterQuery;

	private EntityQuery m_TimeSettingQuery;

	private EntityQuery m_TimeDataQuery;

	private NativeArray<int4> m_HappinessFactors;

	private JobHandle m_LastDeps;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_429327289_0;

	private EntityQuery __query_429327289_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	private static int GetFactorIndex(HappinessFactor factor, uint updateFrame)
	{
		return (int)factor + (int)(26 * updateFrame);
	}

	public float3 GetHappinessFactor(HappinessFactor factor, DynamicBuffer<HappinessFactorParameterData> parameters, ref ComponentLookup<Locked> locked)
	{
		m_LastDeps.Complete();
		return GetHappinessFactor(factor, m_HappinessFactors, parameters, ref locked);
	}

	private static float3 GetHappinessFactor(HappinessFactor factor, NativeArray<int4> happinessFactors, DynamicBuffer<HappinessFactorParameterData> parameters, ref ComponentLookup<Locked> locked)
	{
		int4 @int = 0;
		for (uint num = 0u; num < 16; num++)
		{
			@int += happinessFactors[GetFactorIndex(factor, num)];
		}
		Entity lockedEntity = parameters[(int)factor].m_LockedEntity;
		if (lockedEntity != Entity.Null && locked.HasEnabledComponent(lockedEntity))
		{
			return 0;
		}
		return ((@int.y > 0) ? new float3((float)@int.x / (2f * (float)@int.y), @int.z / @int.y, @int.w / @int.y) : default(float3)) - parameters[(int)factor].m_BaseLevel;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_HappinessFactors = new NativeArray<int4>(416, Allocator.Persistent);
		m_FactorQueue = new NativeQueue<FactorItem>(Allocator.Persistent);
		m_HealthcareParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_ParkParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ParkParameterData>());
		m_EducationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
		m_TelecomParameterQuery = GetEntityQuery(ComponentType.ReadOnly<TelecomParameterData>());
		m_GarbageParameterQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
		m_PoliceParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadWrite<Citizen>(),
				ComponentType.ReadOnly<HouseholdMember>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_TimeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_HappinessFactorParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HappinessFactorParameterData>());
		m_DebugData = new DebugWatchDistribution();
		RequireForUpdate(m_CitizenQuery);
		RequireForUpdate<ServiceFeeParameterData>();
	}

	public static float GetApartmentWellbeing(float sizePerResident, int level)
	{
		return 0.8f * (4f * (float)(level - 1) + (24.55531f + -70.21f / math.pow(1f + math.pow(sizePerResident / 0.03690514f, 25.2376f), 0.01494523f)));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_DebugData.Dispose();
		m_HappinessFactors.Dispose();
		m_FactorQueue.Dispose();
		base.OnDestroy();
	}

	public static float GetFreetimeWellbeingDifferential(int freetime)
	{
		return 4f / (float)freetime;
	}

	public static float GetFreetimeWellbeing(int freetime)
	{
		return 4f * math.log(math.max(1, freetime)) - 25f;
	}

	public static int GetElectricityFeeHappinessEffect(float relativeFee, in CitizenHappinessParameterData data)
	{
		return (int)math.round((float)math.csum(GetElectricityFeeBonuses(relativeFee, in data)) / 2f);
	}

	public static int2 GetElectricityFeeBonuses(Entity building, ref ComponentLookup<ElectricityConsumer> electricityConsumers, float relativeFee, in CitizenHappinessParameterData data)
	{
		if (electricityConsumers.TryGetComponent(building, out var componentData) && componentData.m_WantedConsumption > 0)
		{
			return GetElectricityFeeBonuses(relativeFee, in data);
		}
		return default(int2);
	}

	public static int2 GetElectricityFeeBonuses(float relativeFee, in CitizenHappinessParameterData data)
	{
		return new int2
		{
			y = (int)math.round(data.m_ElectricityFeeWellbeingEffect.Evaluate(relativeFee))
		};
	}

	public static int2 GetElectricitySupplyBonuses(Entity building, ref ComponentLookup<ElectricityConsumer> electricityConsumers, in CitizenHappinessParameterData data)
	{
		if (electricityConsumers.TryGetComponent(building, out var componentData))
		{
			float num = math.saturate((float)componentData.m_CooldownCounter / data.m_ElectricityPenaltyDelay);
			return new int2
			{
				y = (int)math.round((0f - data.m_ElectricityWellbeingPenalty) * num)
			};
		}
		return default(int2);
	}

	public static int GetWaterFeeHappinessEffect(float relativeFee, in CitizenHappinessParameterData data)
	{
		return (int)math.round((float)math.csum(GetWaterFeeBonuses(relativeFee, in data)) / 2f);
	}

	public static int2 GetWaterFeeBonuses(Entity building, ref ComponentLookup<WaterConsumer> waterConsumers, float relativeFee, in CitizenHappinessParameterData data)
	{
		if (waterConsumers.TryGetComponent(building, out var componentData) && componentData.m_WantedConsumption > 0)
		{
			return GetWaterFeeBonuses(relativeFee, in data);
		}
		return default(int2);
	}

	public static int2 GetWaterFeeBonuses(float relativeFee, in CitizenHappinessParameterData data)
	{
		return new int2
		{
			x = (int)math.round(data.m_WaterFeeHealthEffect.Evaluate(relativeFee)),
			y = (int)math.round(data.m_WaterFeeWellbeingEffect.Evaluate(relativeFee))
		};
	}

	public static int2 GetWaterSupplyBonuses(Entity building, ref ComponentLookup<WaterConsumer> waterConsumers, in CitizenHappinessParameterData data)
	{
		if (waterConsumers.TryGetComponent(building, out var componentData))
		{
			float num = math.saturate((float)(int)componentData.m_FreshCooldownCounter / data.m_WaterPenaltyDelay);
			return new int2
			{
				x = (int)math.round((float)(-data.m_WaterHealthPenalty) * num),
				y = (int)math.round((float)(-data.m_WaterWellbeingPenalty) * num)
			};
		}
		return default(int2);
	}

	public static int2 GetWaterPollutionBonuses(Entity building, ref ComponentLookup<WaterConsumer> waterConsumers, DynamicBuffer<CityModifier> cityModifiers, in CitizenHappinessParameterData data)
	{
		int2 result = default(int2);
		if (waterConsumers.HasComponent(building))
		{
			WaterConsumer waterConsumer = waterConsumers[building];
			if (waterConsumer.m_Pollution > 0f)
			{
				float value = 1f;
				CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.PollutionHealthAffect);
				result.x = Mathf.RoundToInt(value * data.m_WaterPollutionBonusMultiplier * math.min(1f, 10f * waterConsumer.m_Pollution));
			}
		}
		return result;
	}

	public static int2 GetSewageBonuses(Entity building, ref ComponentLookup<WaterConsumer> waterConsumers, in CitizenHappinessParameterData data)
	{
		if (waterConsumers.TryGetComponent(building, out var componentData))
		{
			float num = math.saturate((float)(int)componentData.m_SewageCooldownCounter / data.m_SewagePenaltyDelay);
			return new int2
			{
				x = (int)math.round((float)(-data.m_SewageHealthEffect) * num),
				y = (int)math.round((float)(-data.m_SewageWellbeingEffect) * num)
			};
		}
		return default(int2);
	}

	public static int2 GetHealthcareBonuses(float curvePosition, DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage, ref ComponentLookup<Locked> locked, Entity healthcareService, in CitizenHappinessParameterData data)
	{
		if (locked.HasEnabledComponent(healthcareService))
		{
			return new int2(0, 0);
		}
		int2 result = default(int2);
		float serviceCoverage2 = NetUtils.GetServiceCoverage(serviceCoverage, CoverageService.Healthcare, curvePosition);
		result.x = Mathf.RoundToInt(data.m_HealthCareHealthMultiplier * serviceCoverage2);
		result.y = Mathf.RoundToInt(data.m_HealthCareWellbeingMultiplier * serviceCoverage2);
		return result;
	}

	public static int2 GetEducationBonuses(float curvePosition, DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage, ref ComponentLookup<Locked> locked, Entity educationService, in CitizenHappinessParameterData data, int children)
	{
		if (locked.HasEnabledComponent(educationService))
		{
			return new int2(0, 0);
		}
		int2 result = default(int2);
		float f = math.sqrt(children) * data.m_EducationWellbeingMultiplier * (NetUtils.GetServiceCoverage(serviceCoverage, CoverageService.Education, curvePosition) - data.m_NeutralEducation);
		result.y = Mathf.RoundToInt(f);
		return result;
	}

	public static int2 GetEntertainmentBonuses(float curvePosition, DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage, DynamicBuffer<CityModifier> cityModifiers, ref ComponentLookup<Locked> locked, Entity entertainmentService, in CitizenHappinessParameterData data)
	{
		if (locked.HasEnabledComponent(entertainmentService))
		{
			return new int2(0, 0);
		}
		int2 result = default(int2);
		float value = NetUtils.GetServiceCoverage(serviceCoverage, CoverageService.Park, curvePosition);
		CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.Entertainment);
		value = data.m_EntertainmentWellbeingMultiplier * math.min(1f, math.sqrt(value / 1.5f));
		result.x = 0;
		result.y = Mathf.RoundToInt(value);
		return result;
	}

	public static int2 GetGroundPollutionBonuses(Entity building, ref ComponentLookup<Game.Objects.Transform> transforms, NativeArray<GroundPollution> pollutionMap, DynamicBuffer<CityModifier> cityModifiers, in CitizenHappinessParameterData data)
	{
		int2 result = default(int2);
		if (transforms.HasComponent(building))
		{
			short y = (short)(GroundPollutionSystem.GetPollution(transforms[building].m_Position, pollutionMap).m_Pollution / data.m_PollutionBonusDivisor);
			float value = 1f;
			CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.PollutionHealthAffect);
			result.x = (int)((float)(-math.min(data.m_MaxAirAndGroundPollutionBonus, y)) * value);
		}
		return result;
	}

	public static int2 GetAirPollutionBonuses(Entity building, ref ComponentLookup<Game.Objects.Transform> transforms, NativeArray<AirPollution> airPollutionMap, DynamicBuffer<CityModifier> cityModifiers, in CitizenHappinessParameterData data)
	{
		int2 result = default(int2);
		if (transforms.HasComponent(building))
		{
			short y = (short)(AirPollutionSystem.GetPollution(transforms[building].m_Position, airPollutionMap).m_Pollution / data.m_PollutionBonusDivisor);
			float value = 1f;
			CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.PollutionHealthAffect);
			result.x = (int)((float)(-math.min(data.m_MaxAirAndGroundPollutionBonus, y)) * value);
		}
		return result;
	}

	public static int2 GetNoiseBonuses(Entity building, ref ComponentLookup<Game.Objects.Transform> transforms, NativeArray<NoisePollution> noiseMap, in CitizenHappinessParameterData data)
	{
		int2 result = default(int2);
		if (transforms.HasComponent(building))
		{
			short y = (short)(NoisePollutionSystem.GetPollution(transforms[building].m_Position, noiseMap).m_Pollution / data.m_PollutionBonusDivisor);
			result.y = -math.min(data.m_MaxNoisePollutionBonus, y);
		}
		return result;
	}

	public static int2 GetGarbageBonuses(Entity building, ref ComponentLookup<GarbageProducer> garbages, ref ComponentLookup<Locked> locked, Entity garbageService, in GarbageParameterData data)
	{
		if (locked.HasEnabledComponent(garbageService))
		{
			return new int2(0, 0);
		}
		int2 result = default(int2);
		if (garbages.HasComponent(building))
		{
			int y = math.max(0, (garbages[building].m_Garbage - data.m_HappinessEffectBaseline) / data.m_HappinessEffectStep);
			result.x = -math.min(10, y);
			result.y = -math.min(10, y);
		}
		return result;
	}

	public static int2 GetCrimeBonuses(CrimeVictim crimeVictim, Entity building, ref ComponentLookup<CrimeProducer> crimes, ref ComponentLookup<Locked> locked, Entity policeService, in CitizenHappinessParameterData data)
	{
		if (locked.HasEnabledComponent(policeService))
		{
			return new int2(0, 0);
		}
		int2 result = default(int2);
		if (crimes.HasComponent(building))
		{
			int y = Mathf.RoundToInt(math.max(0f, (crimes[building].m_Crime - (float)data.m_NegligibleCrime) * data.m_CrimeMultiplier));
			result.x = 0;
			result.y = -math.min(data.m_MaxCrimePenalty, y);
		}
		result.y -= crimeVictim.m_Effect;
		return result;
	}

	public static int2 GetMailBonuses(Entity building, ref ComponentLookup<MailProducer> mails, ref ComponentLookup<Locked> locked, Entity telecomService, in CitizenHappinessParameterData data)
	{
		if (locked.HasEnabledComponent(telecomService))
		{
			return new int2(0, 0);
		}
		int2 result = default(int2);
		if (mails.HasComponent(building))
		{
			MailProducer mailProducer = mails[building];
			int num = math.max(0, math.max(mailProducer.m_SendingMail, mailProducer.receivingMail) - data.m_NegligibleMail);
			result.x = 0;
			if (num < 25)
			{
				if (!mailProducer.mailDelivered)
				{
					return result;
				}
				int num2 = 125;
				int num3 = 25 - num;
				result.y = (num3 * num3 + (num2 >> 1)) / num2;
			}
			else
			{
				int num4 = 250;
				int num5 = math.min(50, num - 25);
				result.y = -((num5 * num5 + (num4 >> 1)) / num4);
			}
			result.y *= Mathf.RoundToInt(data.m_MailMultiplier);
		}
		return result;
	}

	public static int2 GetTelecomBonuses(Entity building, ref ComponentLookup<Game.Objects.Transform> transforms, CellMapData<TelecomCoverage> telecomCoverage, ref ComponentLookup<Locked> locked, Entity telecomService, in CitizenHappinessParameterData data)
	{
		if (locked.HasEnabledComponent(telecomService))
		{
			return default(int2);
		}
		int2 result = default(int2);
		if (transforms.HasComponent(building))
		{
			float3 position = transforms[building].m_Position;
			float num = TelecomCoverage.SampleNetworkQuality(telecomCoverage, position);
			float telecomBaseline = data.m_TelecomBaseline;
			if (num >= telecomBaseline)
			{
				float num2 = (num - telecomBaseline) / (1f - telecomBaseline);
				result.y = Mathf.RoundToInt(num2 * num2 * data.m_TelecomBonusMultiplier);
			}
			else
			{
				float num3 = 1f - num / telecomBaseline;
				result.y = Mathf.RoundToInt(num3 * num3 * (0f - data.m_TelecomPenaltyMultiplier));
			}
		}
		return result;
	}

	public static int2 GetUnemploymentBonuses(ref Citizen citizen, in CitizenHappinessParameterData data)
	{
		bool num = (citizen.m_State & CitizenFlags.Tourist) != 0;
		int2 result = default(int2);
		if (!num)
		{
			int y = (int)math.max(0f, citizen.m_UnemploymentTimeCounter * data.m_UnemployedWellbeingPenaltyAccumulatePerDay);
			result.y = -math.min(data.m_MaxAccumulatedUnemployedWellbeingPenalty, y);
		}
		return result;
	}

	public static int2 GetTaxBonuses(int educationLevel, NativeArray<int> taxRates, DynamicBuffer<CityModifier> cityModifiers, in CitizenHappinessParameterData data)
	{
		int residentialTaxRate = TaxSystem.GetResidentialTaxRate(educationLevel, taxRates);
		float num = 0f;
		switch (educationLevel)
		{
		case 0:
			num = data.m_TaxUneducatedMultiplier;
			break;
		case 1:
			num = data.m_TaxPoorlyEducatedMultiplier;
			break;
		case 2:
			num = data.m_TaxEducatedMultiplier;
			break;
		case 3:
			num = data.m_TaxWellEducatedMultiplier;
			break;
		case 4:
			num = data.m_TaxHighlyEducatedMultiplier;
			break;
		}
		float value = 10 - residentialTaxRate;
		CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.TaxHappiness);
		value *= 0f - num;
		return new int2(0, Mathf.RoundToInt(value));
	}

	public static int2 GetWellfareBonuses(float curvePosition, DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage, in CitizenHappinessParameterData data, int currentHappiness)
	{
		int2 result = default(int2);
		float num = data.m_WelfareMultiplier * NetUtils.GetServiceCoverage(serviceCoverage, CoverageService.Welfare, curvePosition);
		result.y = Mathf.RoundToInt(num * (float)math.max(0, (50 - currentHappiness) / 50));
		return result;
	}

	public static float GetWelfareValue(float curvePosition, DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage, in CitizenHappinessParameterData data)
	{
		return data.m_WelfareMultiplier * NetUtils.GetServiceCoverage(serviceCoverage, CoverageService.Welfare, curvePosition);
	}

	public static int2 GetCachedWelfareBonuses(float cachedValue, int currentHappiness)
	{
		return new int2
		{
			y = Mathf.RoundToInt(cachedValue * (float)math.max(0, (50 - currentHappiness) / 50))
		};
	}

	public static int2 GetSicknessBonuses(bool hasHealthProblem, ref Citizen citizen, in CitizenHappinessParameterData data)
	{
		if (hasHealthProblem)
		{
			if (citizen.m_SicknessPenalty == 0)
			{
				citizen.m_SicknessPenalty = citizen.m_Health / 2;
			}
			return new int2(-citizen.m_SicknessPenalty, 0);
		}
		citizen.m_SicknessPenalty = 0;
		return default(int2);
	}

	public static int2 GetHomelessBonuses(in CitizenHappinessParameterData data)
	{
		return new int2(data.m_HomelessHealthEffect, data.m_HomelessWellbeingEffect);
	}

	public static int2 GetDeathPenalty(DynamicBuffer<HouseholdCitizen> householdCitizens, ref ComponentLookup<HealthProblem> healthProblems, in CitizenHappinessParameterData data)
	{
		bool flag = false;
		foreach (HouseholdCitizen item in householdCitizens)
		{
			if (CitizenUtils.IsDead(item.m_Citizen, ref healthProblems))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return new int2(-data.m_DeathHealthPenalty, -data.m_DeathWellbeingPenalty);
		}
		return default(int2);
	}

	public static int2 GetLocalEffectBonuses(ref float wellbeing, ref float health, ref LocalEffectSystem.ReadData localEffectData, ref ComponentLookup<Game.Objects.Transform> transforms, Entity home)
	{
		float num = wellbeing;
		float num2 = health;
		if (transforms.HasComponent(home))
		{
			Game.Objects.Transform transform = transforms[home];
			localEffectData.ApplyModifier(ref wellbeing, transform.m_Position, LocalModifierType.Wellbeing);
			localEffectData.ApplyModifier(ref health, transform.m_Position, LocalModifierType.Health);
		}
		return new int2(Mathf.RoundToInt(wellbeing - num), Mathf.RoundToInt(health - num2));
	}

	public static float GetConsumptionHappinessDifferential(float dailyConsumption, int citizens)
	{
		if (dailyConsumption <= 0f)
		{
			return 100f;
		}
		float num = dailyConsumption / math.max(1f, citizens);
		return 8f / (1f + 0.2f * num) - 50000f * math.pow(2f * num + 190f, -2f);
	}

	public static int2 GetConsumptionBonuses(float dailyConsumption, int citizens, in CitizenHappinessParameterData data)
	{
		float num = dailyConsumption / math.max(1f, citizens);
		float f = 20f * math.log(1f + 0.2f * num) + 12500f / (2f * num + 190f) - 112f;
		return new int2(0, math.clamp(Mathf.RoundToInt(f), -40, 40));
	}

	public static int2 GetLeisureBonuses(Citizen citizen)
	{
		if ((citizen.m_State & CitizenFlags.Tourist) != CitizenFlags.None)
		{
			return new int2(0, 7);
		}
		return new int2(0, (citizen.m_LeisureCounter - 128) / 16);
	}

	public static int2 GetLeisureBonuses(int leisureCounter, bool isTourist)
	{
		if (isTourist)
		{
			return new int2(0, 7);
		}
		return new int2(0, (leisureCounter - 128) / 16);
	}

	public static int GetMaxHealth(float ageInYears)
	{
		if (ageInYears < 2f)
		{
			return 100;
		}
		if (ageInYears < 3f)
		{
			return 90;
		}
		if (ageInYears < 6f)
		{
			return 80;
		}
		return 80 - 10 * Mathf.FloorToInt(ageInYears - 5f);
	}

	public static void GetBuildingHappinessFactors(Entity property, NativeArray<int> factors, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<SpawnableBuildingData> spawnableBuildings, ref ComponentLookup<BuildingPropertyData> buildingPropertyDatas, ref ComponentLookup<ConsumptionData> consumptionDatas, ref BufferLookup<CityModifier> cityModifiers, ref ComponentLookup<Building> buildings, ref ComponentLookup<ElectricityConsumer> electricityConsumers, ref ComponentLookup<WaterConsumer> waterConsumers, ref BufferLookup<Game.Net.ServiceCoverage> serviceCoverages, ref ComponentLookup<Locked> locked, ref ComponentLookup<Game.Objects.Transform> transforms, ref ComponentLookup<GarbageProducer> garbageProducers, ref ComponentLookup<CrimeProducer> crimeProducers, ref ComponentLookup<MailProducer> mailProducers, ref ComponentLookup<OfficeBuilding> officeBuildings, ref BufferLookup<Renter> renters, ref ComponentLookup<Citizen> citizenDatas, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<CompanyData> companies, ref ComponentLookup<IndustrialProcessData> industrialProcessDatas, ref ComponentLookup<WorkProvider> workProviders, ref BufferLookup<Employee> employees, ref ComponentLookup<WorkplaceData> workplaceDatas, ref ComponentLookup<Citizen> citizens, ref ComponentLookup<HealthProblem> healthProblems, ref ComponentLookup<ServiceAvailable> serviceAvailables, ref ComponentLookup<ResourceData> resourceDatas, ref ComponentLookup<ZonePropertiesData> zonePropertiesDatas, ref BufferLookup<Efficiency> efficiencies, ref ComponentLookup<ServiceCompanyData> serviceCompanyDatas, ref BufferLookup<ResourceAvailability> availabilities, ref BufferLookup<TradeCost> tradeCosts, CitizenHappinessParameterData citizenHappinessParameters, GarbageParameterData garbageParameters, HealthcareParameterData healthcareParameters, ParkParameterData parkParameters, EducationParameterData educationParameters, TelecomParameterData telecomParameters, ref EconomyParameterData economyParameters, DynamicBuffer<HappinessFactorParameterData> happinessFactorParameters, NativeArray<GroundPollution> pollutionMap, NativeArray<NoisePollution> noisePollutionMap, NativeArray<AirPollution> airPollutionMap, CellMapData<TelecomCoverage> telecomCoverage, Entity city, NativeArray<int> taxRates, NativeArray<Entity> processes, ResourcePrefabs resourcePrefabs, float relativeElectricityFee, float relativeWaterFee)
	{
		for (int i = 0; i < factors.Length; i++)
		{
			factors[i] = 0;
		}
		if (!prefabs.HasComponent(property))
		{
			return;
		}
		Entity prefab = prefabs[property].m_Prefab;
		if (!spawnableBuildings.HasComponent(prefab) || !buildingDatas.HasComponent(prefab))
		{
			return;
		}
		BuildingPropertyData buildingPropertyData = buildingPropertyDatas[prefab];
		DynamicBuffer<CityModifier> cityModifiers2 = cityModifiers[city];
		BuildingData buildingData = buildingDatas[prefab];
		float num = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
		Entity entity = Entity.Null;
		float curvePosition = 0f;
		SpawnableBuildingData spawnableData = spawnableBuildings[prefab];
		int level = spawnableData.m_Level;
		Building building = default(Building);
		if (buildings.HasComponent(property))
		{
			building = buildings[property];
			entity = building.m_RoadEdge;
			curvePosition = building.m_CurvePosition;
		}
		bool flag = false;
		Entity entity2 = default(Entity);
		Entity entity3 = default(Entity);
		IndustrialProcessData processData = default(IndustrialProcessData);
		ServiceCompanyData serviceCompanyData = default(ServiceCompanyData);
		Resource resource = buildingPropertyData.m_AllowedManufactured | buildingPropertyData.m_AllowedSold;
		if (resource != Resource.NoResource)
		{
			if (renters.HasBuffer(property))
			{
				DynamicBuffer<Renter> dynamicBuffer = renters[property];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					entity2 = dynamicBuffer[j].m_Renter;
					if (!companies.HasComponent(entity2) || !prefabs.HasComponent(entity2))
					{
						continue;
					}
					entity3 = prefabs[entity2].m_Prefab;
					if (industrialProcessDatas.HasComponent(entity3))
					{
						if (serviceCompanyDatas.HasComponent(entity3))
						{
							serviceCompanyData = serviceCompanyDatas[entity3];
						}
						processData = industrialProcessDatas[entity3];
						flag = true;
						break;
					}
				}
			}
			int num2 = 0;
			if (flag)
			{
				AddCompanyHappinessFactors(factors, property, prefab, entity2, entity3, processData, serviceCompanyData, buildingPropertyData.m_AllowedSold != Resource.NoResource, level, ref officeBuildings, ref workProviders, ref employees, ref workplaceDatas, ref serviceAvailables, ref resourceDatas, ref efficiencies, ref buildingPropertyDatas, ref availabilities, ref tradeCosts, taxRates, building, spawnableData, buildingData, resourcePrefabs, ref economyParameters);
				num2++;
			}
			else
			{
				for (int k = 0; k < processes.Length; k++)
				{
					processData = industrialProcessDatas[processes[k]];
					bool num3 = buildingPropertyData.m_AllowedSold != Resource.NoResource;
					if (num3 && serviceCompanyDatas.HasComponent(processes[k]))
					{
						serviceCompanyData = serviceCompanyDatas[processes[k]];
					}
					if ((!num3 || serviceCompanyDatas.HasComponent(processes[k])) && (resource & processData.m_Output.m_Resource) != Resource.NoResource)
					{
						AddCompanyHappinessFactors(factors, property, prefab, entity2, entity3, processData, serviceCompanyData, buildingPropertyData.m_AllowedSold != Resource.NoResource, level, ref officeBuildings, ref workProviders, ref employees, ref workplaceDatas, ref serviceAvailables, ref resourceDatas, ref efficiencies, ref buildingPropertyDatas, ref availabilities, ref tradeCosts, taxRates, building, spawnableData, buildingData, resourcePrefabs, ref economyParameters);
						num2++;
					}
				}
			}
			for (int l = 0; l < factors.Length; l++)
			{
				factors[l] /= num2;
			}
		}
		if (buildingPropertyData.m_ResidentialProperties <= 0)
		{
			return;
		}
		for (int m = 0; m < factors.Length; m++)
		{
			factors[m] = Mathf.RoundToInt((float)factors[m] / (1f - economyParameters.m_MixedBuildingCompanyRentPercentage));
		}
		num /= (float)buildingPropertyData.m_ResidentialProperties;
		float num4 = 1f;
		int currentHappiness = 50;
		int leisureCounter = 128;
		float num5 = 0.3f;
		float num6 = 0.25f;
		float num7 = 0.25f;
		float num8 = 0.15f;
		float num9 = 0.05f;
		float num10 = 2f;
		bool isTourist = false;
		if (renters.HasBuffer(property))
		{
			num5 = 0f;
			num6 = 0f;
			num7 = 0f;
			num8 = 0f;
			num9 = 0f;
			int2 @int = default(int2);
			int2 int2 = default(int2);
			int num11 = 0;
			int num12 = 0;
			DynamicBuffer<Renter> dynamicBuffer2 = renters[property];
			for (int n = 0; n < dynamicBuffer2.Length; n++)
			{
				Entity renter = dynamicBuffer2[n].m_Renter;
				if (!householdCitizens.HasBuffer(renter))
				{
					continue;
				}
				num12++;
				DynamicBuffer<HouseholdCitizen> dynamicBuffer3 = householdCitizens[renter];
				for (int num13 = 0; num13 < dynamicBuffer3.Length; num13++)
				{
					Entity citizen = dynamicBuffer3[num13].m_Citizen;
					if (citizenDatas.HasComponent(citizen))
					{
						Citizen citizen2 = citizenDatas[citizen];
						int2.x += citizen2.Happiness;
						int2.y++;
						num11 += citizen2.m_LeisureCounter;
						isTourist = (citizen2.m_State & CitizenFlags.Tourist) != 0;
						switch (citizen2.GetEducationLevel())
						{
						case 0:
							num5 += 1f;
							break;
						case 1:
							num6 += 1f;
							break;
						case 2:
							num7 += 1f;
							break;
						case 3:
							num8 += 1f;
							break;
						case 4:
							num9 += 1f;
							break;
						}
						if (citizen2.GetAge() == CitizenAge.Child)
						{
							@int.x++;
						}
					}
				}
				@int.y++;
			}
			if (@int.y > 0)
			{
				num4 = @int.x / @int.y;
			}
			if (int2.y > 0)
			{
				currentHappiness = Mathf.RoundToInt(int2.x / int2.y);
				leisureCounter = Mathf.RoundToInt(num11 / int2.y);
				num5 /= (float)int2.y;
				num6 /= (float)int2.y;
				num7 /= (float)int2.y;
				num8 /= (float)int2.y;
				num9 /= (float)int2.y;
				num10 = (float)int2.y / (float)num12;
			}
		}
		Entity healthcareServicePrefab = healthcareParameters.m_HealthcareServicePrefab;
		Entity parkServicePrefab = parkParameters.m_ParkServicePrefab;
		Entity educationServicePrefab = educationParameters.m_EducationServicePrefab;
		Entity telecomServicePrefab = telecomParameters.m_TelecomServicePrefab;
		if (!locked.HasEnabledComponent(happinessFactorParameters[4].m_LockedEntity))
		{
			int2 electricitySupplyBonuses = GetElectricitySupplyBonuses(property, ref electricityConsumers, in citizenHappinessParameters);
			factors[3] = (electricitySupplyBonuses.x + electricitySupplyBonuses.y) / 2 - happinessFactorParameters[4].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[23].m_LockedEntity))
		{
			int2 electricityFeeBonuses = GetElectricityFeeBonuses(property, ref electricityConsumers, relativeElectricityFee, in citizenHappinessParameters);
			factors[26] = (electricityFeeBonuses.x + electricityFeeBonuses.y) / 2 - happinessFactorParameters[23].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[8].m_LockedEntity))
		{
			int2 waterSupplyBonuses = GetWaterSupplyBonuses(property, ref waterConsumers, in citizenHappinessParameters);
			factors[7] = (waterSupplyBonuses.x + waterSupplyBonuses.y) / 2 - happinessFactorParameters[8].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[24].m_LockedEntity))
		{
			int2 waterFeeBonuses = GetWaterFeeBonuses(property, ref waterConsumers, relativeWaterFee, in citizenHappinessParameters);
			factors[27] = (waterFeeBonuses.x + waterFeeBonuses.y) / 2 - happinessFactorParameters[24].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[9].m_LockedEntity))
		{
			int2 waterPollutionBonuses = GetWaterPollutionBonuses(property, ref waterConsumers, cityModifiers2, in citizenHappinessParameters);
			factors[8] = (waterPollutionBonuses.x + waterPollutionBonuses.y) / 2 - happinessFactorParameters[9].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[10].m_LockedEntity))
		{
			int2 sewageBonuses = GetSewageBonuses(property, ref waterConsumers, in citizenHappinessParameters);
			factors[9] = (sewageBonuses.x + sewageBonuses.y) / 2 - happinessFactorParameters[10].m_BaseLevel;
		}
		if (serviceCoverages.HasBuffer(entity))
		{
			DynamicBuffer<Game.Net.ServiceCoverage> serviceCoverage = serviceCoverages[entity];
			if (!locked.HasEnabledComponent(happinessFactorParameters[5].m_LockedEntity))
			{
				int2 healthcareBonuses = GetHealthcareBonuses(curvePosition, serviceCoverage, ref locked, healthcareServicePrefab, in citizenHappinessParameters);
				factors[4] = (healthcareBonuses.x + healthcareBonuses.y) / 2 - happinessFactorParameters[5].m_BaseLevel;
			}
			if (!locked.HasEnabledComponent(happinessFactorParameters[12].m_LockedEntity))
			{
				int2 entertainmentBonuses = GetEntertainmentBonuses(curvePosition, serviceCoverage, cityModifiers2, ref locked, parkServicePrefab, in citizenHappinessParameters);
				factors[11] = (entertainmentBonuses.x + entertainmentBonuses.y) / 2 - happinessFactorParameters[12].m_BaseLevel;
			}
			if (!locked.HasEnabledComponent(happinessFactorParameters[13].m_LockedEntity))
			{
				int2 educationBonuses = GetEducationBonuses(curvePosition, serviceCoverage, ref locked, educationServicePrefab, in citizenHappinessParameters, 1);
				factors[12] = Mathf.RoundToInt(num4 * (float)(educationBonuses.x + educationBonuses.y) / 2f) - happinessFactorParameters[13].m_BaseLevel;
			}
			if (!locked.HasEnabledComponent(happinessFactorParameters[15].m_LockedEntity))
			{
				int2 wellfareBonuses = GetWellfareBonuses(curvePosition, serviceCoverage, in citizenHappinessParameters, currentHappiness);
				factors[14] = (wellfareBonuses.x + wellfareBonuses.y) / 2 - happinessFactorParameters[15].m_BaseLevel;
			}
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[6].m_LockedEntity))
		{
			int2 groundPollutionBonuses = GetGroundPollutionBonuses(property, ref transforms, pollutionMap, cityModifiers2, in citizenHappinessParameters);
			factors[5] = (groundPollutionBonuses.x + groundPollutionBonuses.y) / 2 - happinessFactorParameters[6].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[2].m_LockedEntity))
		{
			int2 airPollutionBonuses = GetAirPollutionBonuses(property, ref transforms, airPollutionMap, cityModifiers2, in citizenHappinessParameters);
			factors[2] = (airPollutionBonuses.x + airPollutionBonuses.y) / 2 - happinessFactorParameters[2].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[7].m_LockedEntity))
		{
			int2 noiseBonuses = GetNoiseBonuses(property, ref transforms, noisePollutionMap, in citizenHappinessParameters);
			factors[6] = (noiseBonuses.x + noiseBonuses.y) / 2 - happinessFactorParameters[7].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[11].m_LockedEntity))
		{
			int2 garbageBonuses = GetGarbageBonuses(property, ref garbageProducers, ref locked, happinessFactorParameters[11].m_LockedEntity, in garbageParameters);
			factors[10] = (garbageBonuses.x + garbageBonuses.y) / 2 - happinessFactorParameters[11].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[1].m_LockedEntity))
		{
			int2 crimeBonuses = GetCrimeBonuses(default(CrimeVictim), property, ref crimeProducers, ref locked, happinessFactorParameters[1].m_LockedEntity, in citizenHappinessParameters);
			factors[1] = (crimeBonuses.x + crimeBonuses.y) / 2 - happinessFactorParameters[1].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[14].m_LockedEntity))
		{
			int2 mailBonuses = GetMailBonuses(property, ref mailProducers, ref locked, telecomServicePrefab, in citizenHappinessParameters);
			factors[13] = (mailBonuses.x + mailBonuses.y) / 2 - happinessFactorParameters[14].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[0].m_LockedEntity))
		{
			int2 telecomBonuses = GetTelecomBonuses(property, ref transforms, telecomCoverage, ref locked, telecomServicePrefab, in citizenHappinessParameters);
			factors[0] = (telecomBonuses.x + telecomBonuses.y) / 2 - happinessFactorParameters[0].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[16].m_LockedEntity))
		{
			int2 leisureBonuses = GetLeisureBonuses(leisureCounter, isTourist);
			factors[15] = (leisureBonuses.x + leisureBonuses.y) / 2 - happinessFactorParameters[16].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[17].m_LockedEntity))
		{
			float2 @float = new float2(num5, num5) * GetTaxBonuses(0, taxRates, cityModifiers2, in citizenHappinessParameters) + new float2(num6, num6) * GetTaxBonuses(1, taxRates, cityModifiers2, in citizenHappinessParameters) + new float2(num7, num7) * GetTaxBonuses(2, taxRates, cityModifiers2, in citizenHappinessParameters) + new float2(num8, num8) * GetTaxBonuses(3, taxRates, cityModifiers2, in citizenHappinessParameters) + new float2(num9, num9) * GetTaxBonuses(4, taxRates, cityModifiers2, in citizenHappinessParameters);
			factors[16] = Mathf.RoundToInt(@float.x + @float.y) / 2 - happinessFactorParameters[17].m_BaseLevel;
		}
		if (!locked.HasEnabledComponent(happinessFactorParameters[3].m_LockedEntity))
		{
			float2 float2 = GetApartmentWellbeing(buildingPropertyData.m_SpaceMultiplier * num / num10, level);
			factors[21] = Mathf.RoundToInt(float2.x + float2.y) / 2 - happinessFactorParameters[3].m_BaseLevel;
		}
		if (resource != Resource.NoResource)
		{
			for (int num14 = 0; num14 < factors.Length; num14++)
			{
				factors[num14] = Mathf.RoundToInt((float)factors[num14] * (1f - economyParameters.m_MixedBuildingCompanyRentPercentage));
			}
		}
	}

	private static void AddCompanyHappinessFactors(NativeArray<int> factors, Entity property, Entity prefab, Entity renter, Entity renterPrefab, IndustrialProcessData processData, ServiceCompanyData serviceCompanyData, bool commercial, int level, ref ComponentLookup<OfficeBuilding> officeBuildings, ref ComponentLookup<WorkProvider> workProviders, ref BufferLookup<Employee> employees, ref ComponentLookup<WorkplaceData> workplaceDatas, ref ComponentLookup<ServiceAvailable> serviceAvailables, ref ComponentLookup<ResourceData> resourceDatas, ref BufferLookup<Efficiency> efficiencies, ref ComponentLookup<BuildingPropertyData> buildingPropertyDatas, ref BufferLookup<ResourceAvailability> availabilities, ref BufferLookup<TradeCost> tradeCosts, NativeArray<int> taxRates, Building building, SpawnableBuildingData spawnableData, BuildingData buildingData, ResourcePrefabs resourcePrefabs, ref EconomyParameterData economyParameters)
	{
	}

	private static int GetFactor(float profit, float defaultProfit)
	{
		return Mathf.RoundToInt(10f * (profit / defaultProfit - 1f));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		m_CitizenQuery.ResetFilter();
		m_CitizenQuery.AddSharedComponentFilter(new UpdateFrame(updateFrameWithInterval));
		NativeQueue<int>.ParallelWriter debugQueue = default(NativeQueue<int>.ParallelWriter);
		if (m_DebugData.IsEnabled)
		{
			debugQueue = m_DebugData.GetQueue(clear: false, out var _).AsParallelWriter();
		}
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		JobHandle dependencies5;
		JobHandle deps2;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CitizenHappinessJob
		{
			m_DebugQueue = debugQueue,
			m_DebugOn = m_DebugData.IsEnabled,
			m_CitizenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CrimeVictimType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CrimeVictim_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CriminalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Criminal_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StudentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HealthProblemType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Properties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Garbages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Locked = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CrimeProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceFees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
			m_Prisons = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Prison_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Schools = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_School_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies),
			m_AirPollutionMap = m_AirPollutionSystem.GetMap(readOnly: true, out dependencies2),
			m_NoisePollutionMap = m_NoisePollutionSystem.GetMap(readOnly: true, out dependencies3),
			m_TelecomCoverage = m_TelecomCoverageSystem.GetData(readOnly: true, out dependencies4),
			m_LocalEffectData = m_LocalEffectSystem.GetReadData(out dependencies5),
			m_HealthcareParameters = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
			m_ParkParameters = m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
			m_EducationParameters = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
			m_TelecomParameters = m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),
			m_GarbageParameters = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
			m_PoliceParameters = m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>(),
			m_CitizenHappinessParameters = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
			m_LeisureParameters = __query_429327289_0.GetSingleton<LeisureParametersData>(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_TimeSettings = m_TimeSettingQuery.GetSingleton<TimeSettingsData>(),
			m_FeeParameters = __query_429327289_1.GetSingleton<ServiceFeeParameterData>(),
			m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>(),
			m_TaxRates = m_TaxSystem.GetTaxRates(),
			m_RawUpdateFrame = updateFrameWithInterval,
			m_City = m_CitySystem.City,
			m_RandomSeed = RandomSeed.Next(),
			m_FactorQueue = m_FactorQueue.AsParallelWriter(),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps2).AsParallelWriter()
		}, m_CitizenQuery, JobHandle.CombineDependencies(dependencies5, dependencies4, JobHandle.CombineDependencies(dependencies2, dependencies3, JobHandle.CombineDependencies(base.Dependency, dependencies, deps2))));
		if (m_DebugData.IsEnabled)
		{
			m_DebugData.AddWriter(jobHandle);
		}
		m_GroundPollutionSystem.AddReader(jobHandle);
		m_AirPollutionSystem.AddReader(jobHandle);
		m_NoisePollutionSystem.AddReader(jobHandle);
		m_TelecomCoverageSystem.AddReader(jobHandle);
		m_LocalEffectSystem.AddLocalEffectReader(jobHandle);
		m_TaxSystem.AddReader(jobHandle);
		m_CityStatisticsSystem.AddWriter(jobHandle);
		HappinessFactorJob jobData = new HappinessFactorJob
		{
			m_FactorQueue = m_FactorQueue,
			m_HappinessFactors = m_HappinessFactors,
			m_RawUpdateFrame = updateFrameWithInterval,
			m_TriggerActionQueue = m_TriggerSystem.CreateActionBuffer(),
			m_ParameterEntity = m_HappinessFactorParameterQuery.GetSingletonEntity(),
			m_Parameters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_HappinessFactorParameterData_RO_BufferLookup, ref base.CheckedStateRef),
			m_Locked = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, jobHandle);
		m_LastDeps = base.Dependency;
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
	}

	private static TriggerType GetTriggerTypeForHappinessFactor(HappinessFactor factor)
	{
		switch (factor)
		{
		case HappinessFactor.Telecom:
			return TriggerType.TelecomHappinessFactor;
		case HappinessFactor.Crime:
			return TriggerType.CrimeHappinessFactor;
		case HappinessFactor.AirPollution:
			return TriggerType.AirPollutionHappinessFactor;
		case HappinessFactor.Apartment:
			return TriggerType.ApartmentHappinessFactor;
		case HappinessFactor.Electricity:
			return TriggerType.ElectricityHappinessFactor;
		case HappinessFactor.Healthcare:
			return TriggerType.HealthcareHappinessFactor;
		case HappinessFactor.GroundPollution:
			return TriggerType.GroundPollutionHappinessFactor;
		case HappinessFactor.NoisePollution:
			return TriggerType.NoisePollutionHappinessFactor;
		case HappinessFactor.Water:
			return TriggerType.WaterHappinessFactor;
		case HappinessFactor.WaterPollution:
			return TriggerType.WaterPollutionHappinessFactor;
		case HappinessFactor.Sewage:
			return TriggerType.SewageHappinessFactor;
		case HappinessFactor.Garbage:
			return TriggerType.GarbageHappinessFactor;
		case HappinessFactor.Entertainment:
			return TriggerType.EntertainmentHappinessFactor;
		case HappinessFactor.Education:
			return TriggerType.EducationHappinessFactor;
		case HappinessFactor.Mail:
			return TriggerType.MailHappinessFactor;
		case HappinessFactor.Welfare:
			return TriggerType.WelfareHappinessFactor;
		case HappinessFactor.Leisure:
			return TriggerType.LeisureHappinessFactor;
		case HappinessFactor.Tax:
			return TriggerType.TaxHappinessFactor;
		case HappinessFactor.Buildings:
			return TriggerType.BuildingsHappinessFactor;
		case HappinessFactor.Consumption:
			return TriggerType.WealthHappinessFactor;
		case HappinessFactor.TrafficPenalty:
			return TriggerType.TrafficPenaltyHappinessFactor;
		case HappinessFactor.DeathPenalty:
			return TriggerType.DeathPenaltyHappinessFactor;
		case HappinessFactor.Homelessness:
			return TriggerType.HomelessnessHappinessFactor;
		case HappinessFactor.ElectricityFee:
			return TriggerType.ElectricityFeeHappinessFactor;
		case HappinessFactor.WaterFee:
			return TriggerType.WaterFeeHappinessFactor;
		case HappinessFactor.Unemployment:
			return TriggerType.CitizenUnemployedHappinessFactor;
		default:
			UnityEngine.Debug.LogError($"Unknown trigger type for happiness factor: {factor}");
			return TriggerType.NewNotification;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(26);
		for (int i = 0; i < 416; i++)
		{
			int4 value = m_HappinessFactors[i];
			writer.Write(value);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version < Version.happinessFactorSerialization)
		{
			for (int i = 0; i < 352; i++)
			{
				reader.Read(out int4 value);
				m_HappinessFactors[i] = value;
			}
			return;
		}
		reader.Read(out int value2);
		for (int j = 0; j < value2 * 16; j++)
		{
			reader.Read(out int4 value3);
			m_HappinessFactors[j] = value3;
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (serializationContext.purpose == Colossal.Serialization.Entities.Purpose.NewGame)
		{
			for (int i = 0; i < 416; i++)
			{
				m_HappinessFactors[i] = default(int4);
			}
		}
	}

	public void SetDefaults(Context context)
	{
		for (int i = 0; i < 416; i++)
		{
			m_HappinessFactors[i] = default(int4);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<LeisureParametersData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_429327289_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_429327289_1 = entityQueryBuilder2.Build(ref state);
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
	public CitizenHappinessSystem()
	{
	}
}
