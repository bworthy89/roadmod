using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Companies;
using Game.Debug;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Reflection;
using Game.Triggers;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ResidentialDemandSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct UpdateResidentialDemandJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_UnlockedZonePrefabs;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

		[ReadOnly]
		public NativeList<DemandParameterData> m_DemandParameters;

		[ReadOnly]
		public NativeArray<int> m_StudyPositions;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		[ReadOnly]
		public float m_UnemploymentRate;

		public Entity m_City;

		public NativeValue<int> m_HouseholdDemand;

		public NativeValue<int3> m_BuildingDemand;

		public NativeArray<int> m_LowDemandFactors;

		public NativeArray<int> m_MediumDemandFactors;

		public NativeArray<int> m_HighDemandFactors;

		public CountHouseholdDataSystem.HouseholdData m_HouseholdCountData;

		public CountResidentialPropertySystem.ResidentialPropertyData m_ResidentialPropertyData;

		public Workplaces m_FreeWorkplaces;

		public Workplaces m_TotalWorkplaces;

		public NativeQueue<TriggerAction> m_TriggerQueue;

		public float2 m_ResidentialDemandWeightsSelector;

		public bool m_UnlimitedDemand;

		public void Execute()
		{
			bool3 test = default(bool3);
			for (int i = 0; i < m_UnlockedZonePrefabs.Length; i++)
			{
				ZoneData zoneData = m_ZoneDatas[m_UnlockedZonePrefabs[i]];
				if (zoneData.m_AreaType == AreaType.Residential)
				{
					ZonePropertiesData zonePropertiesData = m_ZonePropertiesDatas[m_UnlockedZonePrefabs[i]];
					switch (PropertyUtils.GetZoneDensity(zoneData, zonePropertiesData))
					{
					case ZoneDensity.Low:
						test.x = true;
						break;
					case ZoneDensity.Medium:
						test.y = true;
						break;
					case ZoneDensity.High:
						test.z = true;
						break;
					}
				}
			}
			int3 @int = m_ResidentialPropertyData.m_FreeProperties;
			int3 int2 = m_ResidentialPropertyData.m_TotalProperties;
			DemandParameterData demandParameterData = m_DemandParameters[0];
			int num = 0;
			for (int j = 1; j <= 4; j++)
			{
				num += m_StudyPositions[j];
			}
			Population population = m_Populations[m_City];
			float factorValue = 20f - math.smoothstep(0f, 20f, (float)population.m_Population / 20000f);
			int num2 = math.max(demandParameterData.m_MinimumHappiness, population.m_AverageHappiness);
			float num3 = 0f;
			for (int k = 0; k < 5; k++)
			{
				num3 += (float)(-(TaxSystem.GetResidentialTaxRate(k, m_TaxRates) - 10));
			}
			num3 = demandParameterData.m_TaxEffect.x * (num3 / 5f);
			float factorValue2 = demandParameterData.m_HappinessEffect * (float)(num2 - demandParameterData.m_NeutralHappiness);
			float x = (0f - demandParameterData.m_HomelessEffect) * math.clamp(2f * (float)m_HouseholdCountData.m_HomelessHouseholdCount / (float)demandParameterData.m_NeutralHomelessness, 0f, 2f);
			x = math.min(x, kMaxFactorEffect);
			float valueToClamp = demandParameterData.m_HomelessEffect * math.clamp(2f * (float)m_HouseholdCountData.m_HomelessHouseholdCount / (float)demandParameterData.m_NeutralHomelessness, 0f, 2f);
			valueToClamp = math.clamp(valueToClamp, 0f, kMaxFactorEffect);
			float valueToClamp2 = demandParameterData.m_AvailableWorkplaceEffect * ((float)m_FreeWorkplaces.SimpleWorkplacesCount - (float)m_TotalWorkplaces.SimpleWorkplacesCount * demandParameterData.m_NeutralAvailableWorkplacePercentage / 100f);
			valueToClamp2 = math.clamp(valueToClamp2, 0f, 40f);
			float valueToClamp3 = demandParameterData.m_AvailableWorkplaceEffect * ((float)m_FreeWorkplaces.ComplexWorkplacesCount - (float)m_TotalWorkplaces.ComplexWorkplacesCount * demandParameterData.m_NeutralAvailableWorkplacePercentage / 100f);
			valueToClamp3 = math.clamp(valueToClamp3, 0f, 20f);
			float factorValue3 = demandParameterData.m_StudentEffect * math.clamp((float)num / 200f, 0f, 5f);
			float factorValue4 = demandParameterData.m_NeutralUnemployment - m_UnemploymentRate;
			factorValue = GetFactorValue(factorValue, m_ResidentialDemandWeightsSelector);
			factorValue2 = GetFactorValue(factorValue2, m_ResidentialDemandWeightsSelector);
			x = GetFactorValue(x, m_ResidentialDemandWeightsSelector);
			valueToClamp = GetFactorValue(valueToClamp, m_ResidentialDemandWeightsSelector);
			num3 = GetFactorValue(num3, m_ResidentialDemandWeightsSelector);
			valueToClamp2 = GetFactorValue(valueToClamp2, m_ResidentialDemandWeightsSelector);
			valueToClamp3 = GetFactorValue(valueToClamp3, m_ResidentialDemandWeightsSelector);
			factorValue3 = GetFactorValue(factorValue3, m_ResidentialDemandWeightsSelector);
			factorValue4 = GetFactorValue(factorValue4, m_ResidentialDemandWeightsSelector);
			m_HouseholdDemand.value = (int)math.min(200f, factorValue + factorValue2 + x + num3 + factorValue4 + factorValue3 + math.max(valueToClamp2, valueToClamp3));
			int num4 = Mathf.RoundToInt(100f * (float)(demandParameterData.m_FreeResidentialRequirement.x - @int.x) / (float)demandParameterData.m_FreeResidentialRequirement.x);
			int num5 = Mathf.RoundToInt(100f * (float)(demandParameterData.m_FreeResidentialRequirement.y - @int.y) / (float)demandParameterData.m_FreeResidentialRequirement.y);
			int num6 = Mathf.RoundToInt(100f * (float)(demandParameterData.m_FreeResidentialRequirement.z - @int.z) / (float)demandParameterData.m_FreeResidentialRequirement.z);
			m_LowDemandFactors[7] = Mathf.RoundToInt(factorValue2);
			m_LowDemandFactors[6] = Mathf.RoundToInt(valueToClamp2) / 2;
			m_LowDemandFactors[5] = Mathf.RoundToInt(factorValue4);
			m_LowDemandFactors[11] = Mathf.RoundToInt(num3);
			m_LowDemandFactors[13] = num4;
			m_MediumDemandFactors[7] = Mathf.RoundToInt(factorValue2);
			m_MediumDemandFactors[6] = Mathf.RoundToInt(valueToClamp2);
			m_MediumDemandFactors[5] = Mathf.RoundToInt(factorValue4);
			m_MediumDemandFactors[11] = Mathf.RoundToInt(num3);
			m_MediumDemandFactors[12] = Mathf.RoundToInt(factorValue3);
			m_MediumDemandFactors[13] = num5;
			m_HighDemandFactors[7] = Mathf.RoundToInt(factorValue2);
			m_HighDemandFactors[8] = Mathf.RoundToInt(valueToClamp);
			m_HighDemandFactors[6] = Mathf.RoundToInt(valueToClamp2);
			m_HighDemandFactors[5] = Mathf.RoundToInt(factorValue4);
			m_HighDemandFactors[11] = Mathf.RoundToInt(num3);
			m_HighDemandFactors[12] = Mathf.RoundToInt(factorValue3);
			m_HighDemandFactors[13] = num6;
			int num7 = ((m_LowDemandFactors[13] >= 0) ? (m_LowDemandFactors[7] + m_LowDemandFactors[11] + m_LowDemandFactors[6] + m_LowDemandFactors[5] + m_LowDemandFactors[13]) : 0);
			int num8 = ((m_MediumDemandFactors[13] >= 0) ? (m_MediumDemandFactors[7] + m_MediumDemandFactors[11] + m_MediumDemandFactors[6] + m_MediumDemandFactors[12] + m_MediumDemandFactors[5] + m_MediumDemandFactors[13]) : 0);
			int num9 = ((m_HighDemandFactors[13] >= 0) ? (m_HighDemandFactors[7] + m_HighDemandFactors[8] + m_HighDemandFactors[11] + m_HighDemandFactors[6] + m_HighDemandFactors[12] + m_HighDemandFactors[5] + m_HighDemandFactors[13]) : 0);
			m_LowDemandFactors[13] = ((int2.x > 0) ? num4 : 0);
			m_LowDemandFactors[18] = ((int2.x <= 0) ? num4 : 0);
			m_MediumDemandFactors[13] = ((int2.y > 0) ? num5 : 0);
			m_MediumDemandFactors[18] = ((int2.y <= 0) ? num5 : 0);
			m_HighDemandFactors[13] = ((int2.z > 0) ? num6 : 0);
			m_HighDemandFactors[18] = ((int2.z <= 0) ? num6 : 0);
			if (m_TotalWorkplaces.SimpleWorkplacesCount + m_TotalWorkplaces.ComplexWorkplacesCount <= 0)
			{
				m_LowDemandFactors[6] = ((m_LowDemandFactors[6] <= 0) ? m_LowDemandFactors[6] : 0);
				m_MediumDemandFactors[6] = ((m_MediumDemandFactors[6] <= 0) ? m_MediumDemandFactors[6] : 0);
				m_HighDemandFactors[6] = ((m_HighDemandFactors[6] <= 0) ? m_HighDemandFactors[6] : 0);
			}
			if (population.m_Population == 0)
			{
				m_LowDemandFactors[5] = 0;
				m_MediumDemandFactors[5] = 0;
				m_HighDemandFactors[5] = 0;
			}
			m_BuildingDemand.value = new int3(math.clamp(m_HouseholdDemand.value / 2 + num4 + num7, 0, 100), math.clamp(m_HouseholdDemand.value / 2 + num5 + num8, 0, 100), math.clamp(m_HouseholdDemand.value / 2 + num6 + num9, 0, 100));
			m_BuildingDemand.value = math.select(default(int3), m_BuildingDemand.value, test);
			if (m_UnlimitedDemand)
			{
				m_BuildingDemand.value = 100;
			}
			m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.ResidentialDemand, Entity.Null, (int2.x + int2.y + int2.z > 100) ? ((float)(m_BuildingDemand.value.x + m_BuildingDemand.value.y + m_BuildingDemand.value.z) / 100f) : 0f));
			m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.EmptyBuilding, Entity.Null, (int2.x + int2.y + int2.z > 100) ? ((float)(@int.x + @int.y + @int.z) * 100f / (float)(int2.x + int2.y + int2.z)) : 100f));
		}

		private int GetFactorValue(float factorValue, float2 weightSelector)
		{
			if (!(factorValue < 0f))
			{
				return (int)(factorValue * weightSelector.y);
			}
			return (int)(factorValue * weightSelector.x);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
		}
	}

	public static readonly int kMaxFactorEffect = 15;

	private TaxSystem m_TaxSystem;

	private CountStudyPositionsSystem m_CountStudyPositionsSystem;

	private CountWorkplacesSystem m_CountWorkplacesSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private CountResidentialPropertySystem m_CountResidentialPropertySystem;

	private CitySystem m_CitySystem;

	private TriggerSystem m_TriggerSystem;

	private EntityQuery m_DemandParameterGroup;

	private EntityQuery m_UnlockedZonePrefabQuery;

	private EntityQuery m_GameModeSettingQuery;

	private bool m_UnlimitedDemand;

	[DebugWatchValue(color = "#27ae60")]
	private NativeValue<int> m_HouseholdDemand;

	[DebugWatchValue(color = "#117a65")]
	private NativeValue<int3> m_BuildingDemand;

	[EnumArray(typeof(DemandFactor))]
	[DebugWatchValue]
	private NativeArray<int> m_LowDemandFactors;

	[EnumArray(typeof(DemandFactor))]
	[DebugWatchValue]
	private NativeArray<int> m_MediumDemandFactors;

	[EnumArray(typeof(DemandFactor))]
	[DebugWatchValue]
	private NativeArray<int> m_HighDemandFactors;

	[DebugWatchDeps]
	private JobHandle m_WriteDependencies;

	private JobHandle m_ReadDependencies;

	private int m_LastHouseholdDemand;

	private int3 m_LastBuildingDemand;

	private float2 m_ResidentialDemandWeightsSelector;

	private TypeHandle __TypeHandle;

	public int householdDemand => m_LastHouseholdDemand;

	public int3 buildingDemand => m_LastBuildingDemand;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 10;
	}

	public void SetUnlimitedDemand(bool unlimited)
	{
		m_UnlimitedDemand = unlimited;
	}

	public NativeArray<int> GetLowDensityDemandFactors(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_LowDemandFactors;
	}

	public NativeArray<int> GetMediumDensityDemandFactors(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_MediumDemandFactors;
	}

	public NativeArray<int> GetHighDensityDemandFactors(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_HighDemandFactors;
	}

	public void AddReader(JobHandle reader)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			m_ResidentialDemandWeightsSelector = new float2(1f, 1f);
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable)
		{
			m_ResidentialDemandWeightsSelector = singleton.m_ResidentialDemandWeightsSelector;
		}
		else
		{
			m_ResidentialDemandWeightsSelector = new float2(1f, 1f);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_DemandParameterGroup = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		m_UnlockedZonePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.ReadOnly<ZonePropertiesData>(), ComponentType.Exclude<Locked>());
		m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CountStudyPositionsSystem = base.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
		m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_CountResidentialPropertySystem = base.World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_HouseholdDemand = new NativeValue<int>(Allocator.Persistent);
		m_BuildingDemand = new NativeValue<int3>(Allocator.Persistent);
		m_LowDemandFactors = new NativeArray<int>(19, Allocator.Persistent);
		m_MediumDemandFactors = new NativeArray<int>(19, Allocator.Persistent);
		m_HighDemandFactors = new NativeArray<int>(19, Allocator.Persistent);
		m_ResidentialDemandWeightsSelector = new float2(1f, 1f);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_HouseholdDemand.Dispose();
		m_BuildingDemand.Dispose();
		m_LowDemandFactors.Dispose();
		m_MediumDemandFactors.Dispose();
		m_HighDemandFactors.Dispose();
		base.OnDestroy();
	}

	public void SetDefaults(Context context)
	{
		m_HouseholdDemand.value = 0;
		m_BuildingDemand.value = default(int3);
		m_LowDemandFactors.Fill(0);
		m_MediumDemandFactors.Fill(0);
		m_HighDemandFactors.Fill(0);
		m_LastHouseholdDemand = 0;
		m_LastBuildingDemand = default(int3);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_HouseholdDemand.value;
		writer.Write(value);
		int3 value2 = m_BuildingDemand.value;
		writer.Write(value2);
		int length = m_LowDemandFactors.Length;
		writer.Write(length);
		NativeArray<int> value3 = m_LowDemandFactors;
		writer.Write(value3);
		NativeArray<int> value4 = m_MediumDemandFactors;
		writer.Write(value4);
		NativeArray<int> value5 = m_HighDemandFactors;
		writer.Write(value5);
		int value6 = m_LastHouseholdDemand;
		writer.Write(value6);
		int3 value7 = m_LastBuildingDemand;
		writer.Write(value7);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_HouseholdDemand.value = value;
		if (reader.context.version < Version.residentialDemandSplit)
		{
			reader.Read(out int value2);
			m_BuildingDemand.value = new int3(value2 / 3, value2 / 3, value2 / 3);
		}
		else
		{
			reader.Read(out int3 value3);
			m_BuildingDemand.value = value3;
		}
		if (reader.context.version < Version.demandFactorCountSerialization)
		{
			NativeArray<int> nativeArray = new NativeArray<int>(13, Allocator.Temp);
			NativeArray<int> value4 = nativeArray;
			reader.Read(value4);
			CollectionUtils.CopySafe(nativeArray, m_LowDemandFactors);
			nativeArray.Dispose();
		}
		else
		{
			reader.Read(out int value5);
			if (value5 == m_LowDemandFactors.Length)
			{
				NativeArray<int> value6 = m_LowDemandFactors;
				reader.Read(value6);
				NativeArray<int> value7 = m_MediumDemandFactors;
				reader.Read(value7);
				NativeArray<int> value8 = m_HighDemandFactors;
				reader.Read(value8);
			}
			else
			{
				NativeArray<int> nativeArray2 = new NativeArray<int>(value5, Allocator.Temp);
				NativeArray<int> value9 = nativeArray2;
				reader.Read(value9);
				CollectionUtils.CopySafe(nativeArray2, m_LowDemandFactors);
				NativeArray<int> value10 = nativeArray2;
				reader.Read(value10);
				CollectionUtils.CopySafe(nativeArray2, m_MediumDemandFactors);
				NativeArray<int> value11 = nativeArray2;
				reader.Read(value11);
				CollectionUtils.CopySafe(nativeArray2, m_HighDemandFactors);
				nativeArray2.Dispose();
			}
		}
		ref int value12 = ref m_LastHouseholdDemand;
		reader.Read(out value12);
		if (reader.context.version < Version.residentialDemandSplit)
		{
			reader.Read(out int value13);
			m_LastBuildingDemand = new int3(value13 / 3, value13 / 3, value13 / 3);
		}
		else
		{
			ref int3 value14 = ref m_LastBuildingDemand;
			reader.Read(out value14);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_DemandParameterGroup.IsEmptyIgnoreFilter)
		{
			m_LastHouseholdDemand = m_HouseholdDemand.value;
			m_LastBuildingDemand = m_BuildingDemand.value;
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			JobHandle deps;
			UpdateResidentialDemandJob jobData = new UpdateResidentialDemandJob
			{
				m_UnlockedZonePrefabs = m_UnlockedZonePrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ZonePropertiesDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DemandParameters = m_DemandParameterGroup.ToComponentDataListAsync<DemandParameterData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
				m_StudyPositions = m_CountStudyPositionsSystem.GetStudyPositionsByEducation(out deps),
				m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces(),
				m_TotalWorkplaces = m_CountWorkplacesSystem.GetTotalWorkplaces(),
				m_HouseholdCountData = m_CountHouseholdDataSystem.GetHouseholdCountData(),
				m_ResidentialPropertyData = m_CountResidentialPropertySystem.GetResidentialPropertyData(),
				m_TaxRates = m_TaxSystem.GetTaxRates(),
				m_City = m_CitySystem.City,
				m_HouseholdDemand = m_HouseholdDemand,
				m_BuildingDemand = m_BuildingDemand,
				m_LowDemandFactors = m_LowDemandFactors,
				m_MediumDemandFactors = m_MediumDemandFactors,
				m_HighDemandFactors = m_HighDemandFactors,
				m_UnemploymentRate = m_CountHouseholdDataSystem.UnemploymentRate,
				m_ResidentialDemandWeightsSelector = m_ResidentialDemandWeightsSelector,
				m_TriggerQueue = m_TriggerSystem.CreateActionBuffer(),
				m_UnlimitedDemand = m_UnlimitedDemand
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle2, deps, outJobHandle));
			m_WriteDependencies = base.Dependency;
			m_CountStudyPositionsSystem.AddReader(base.Dependency);
			m_TaxSystem.AddReader(base.Dependency);
			m_TriggerSystem.AddActionBufferWriter(base.Dependency);
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
	public ResidentialDemandSystem()
	{
	}
}
