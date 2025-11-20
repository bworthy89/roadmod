using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class HealthcareInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		CitizenHealth,
		CitizenCount,
		SickCitizens,
		PatientCount,
		PatientCapacity,
		ProcessingRate,
		CemeteryUse,
		CemeteryCapacity,
		ResultCount
	}

	[BurstCompile]
	public struct CalculateAverageHealthJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Household> m_HouseholdType;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Household> nativeArray = chunk.GetNativeArray(ref m_HouseholdType);
			BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				if ((nativeArray[i].m_Flags & HouseholdFlags.MovedIn) == 0)
				{
					continue;
				}
				DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity citizen = dynamicBuffer[j].m_Citizen;
					if (!CitizenUtils.IsDead(citizen, ref m_HealthProblems) && CitizenUtils.TryGetResident(citizen, m_Citizens, out var citizen2))
					{
						if (m_HealthProblems.TryGetComponent(citizen, out var componentData) && (componentData.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Injured)) != HealthProblemFlags.None)
						{
							num3++;
						}
						num2 += citizen2.m_Health;
						num++;
					}
				}
			}
			m_Results[0] += num2;
			m_Results[1] += num;
			m_Results[2] += num3;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateHealthcareJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Patient> m_PatientType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<HospitalData> m_HospitalDatas;

		public NativeArray<float> m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<Patient> bufferAccessor = chunk.GetBufferAccessor(ref m_PatientType);
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Efficiency> bufferAccessor3 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				DynamicBuffer<Patient> dynamicBuffer = bufferAccessor[i];
				if (BuildingUtils.GetEfficiency(bufferAccessor3, i) != 0f)
				{
					HospitalData data = default(HospitalData);
					if (m_HospitalDatas.HasComponent(prefabRef.m_Prefab))
					{
						data = m_HospitalDatas[prefabRef.m_Prefab];
					}
					if (bufferAccessor2.Length != 0)
					{
						UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_Prefabs, ref m_HospitalDatas);
					}
					num += dynamicBuffer.Length;
					num2 += data.m_PatientCapacity;
				}
			}
			m_Result[3] += num;
			m_Result[4] += num2;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateDeathcareJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DeathcareFacility> m_DeathcareFacilityType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> m_DeathcareFacilities;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Buildings.DeathcareFacility> nativeArray = chunk.GetNativeArray(ref m_DeathcareFacilityType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity prefab = nativeArray2[i].m_Prefab;
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				if (efficiency != 0f)
				{
					DeathcareFacilityData data = default(DeathcareFacilityData);
					if (m_DeathcareFacilities.HasComponent(prefab))
					{
						data = m_DeathcareFacilities[prefab];
					}
					if (bufferAccessor2.Length != 0)
					{
						UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_Prefabs, ref m_DeathcareFacilities);
					}
					if (data.m_LongTermStorage)
					{
						num2 += (float)nativeArray[i].m_LongTermStoredCount;
						num3 += (float)data.m_StorageCapacity;
					}
					num += efficiency * data.m_ProcessingRate;
				}
			}
			m_Results[5] += num;
			m_Results[6] += num2;
			m_Results[7] += num3;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Household> __Game_Citizens_Household_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Patient> __Game_Buildings_Patient_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HospitalData> __Game_Prefabs_HospitalData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.DeathcareFacility> __Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> __Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Household_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Household>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Patient_RO_BufferTypeHandle = state.GetBufferTypeHandle<Patient>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_HospitalData_RO_ComponentLookup = state.GetComponentLookup<HospitalData>(isReadOnly: true);
			__Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.DeathcareFacility>(isReadOnly: true);
			__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup = state.GetComponentLookup<DeathcareFacilityData>(isReadOnly: true);
		}
	}

	private const string kGroup = "healthcareInfo";

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ValueBinding<float> m_AverageHealth;

	private ValueBinding<int> m_PatientCount;

	private ValueBinding<int> m_SickCount;

	private ValueBinding<int> m_PatientCapacity;

	private ValueBinding<float> m_DeathRate;

	private ValueBinding<float> m_ProcessingRate;

	private ValueBinding<int> m_CemeteryUse;

	private ValueBinding<int> m_CemeteryCapacity;

	private GetterValueBinding<IndicatorValue> m_HealthcareAvailability;

	private GetterValueBinding<IndicatorValue> m_DeathcareAvailability;

	private GetterValueBinding<IndicatorValue> m_CemeteryAvailability;

	private EntityQuery m_HouseholdQuery;

	private EntityQuery m_DeathcareFacilityQuery;

	private EntityQuery m_HealthcareFacilityQuery;

	private EntityQuery m_DeathcareFacilityModifiedQuery;

	private EntityQuery m_HealthcareFacilityModifiedQuery;

	private NativeArray<float> m_Results;

	private TypeHandle __TypeHandle;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_AverageHealth.active && !m_PatientCount.active && !m_SickCount.active && !m_PatientCapacity.active && !m_HealthcareAvailability.active && !m_DeathRate.active && !m_ProcessingRate.active && !m_CemeteryUse.active && !m_CemeteryCapacity.active && !m_DeathcareAvailability.active)
			{
				return m_CemeteryAvailability.active;
			}
			return true;
		}
	}

	protected override bool Modified
	{
		get
		{
			if (m_DeathcareFacilityModifiedQuery.IsEmptyIgnoreFilter)
			{
				return !m_HealthcareFacilityModifiedQuery.IsEmptyIgnoreFilter;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_HouseholdQuery = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<MovingAway>());
		m_DeathcareFacilityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.DeathcareFacility>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Patient>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_DeathcareFacilityModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[5]
			{
				ComponentType.ReadOnly<Game.Buildings.DeathcareFacility>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<ServiceDispatch>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<Patient>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_HealthcareFacilityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Hospital>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Patient>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HealthcareFacilityModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[5]
			{
				ComponentType.ReadOnly<Game.Buildings.Hospital>(),
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<ServiceDispatch>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<Patient>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		AddBinding(m_AverageHealth = new ValueBinding<float>("healthcareInfo", "averageHealth", 0f));
		AddBinding(m_DeathRate = new ValueBinding<float>("healthcareInfo", "deathRate", 0f));
		AddBinding(m_ProcessingRate = new ValueBinding<float>("healthcareInfo", "processingRate", 0f));
		AddBinding(m_CemeteryUse = new ValueBinding<int>("healthcareInfo", "cemeteryUse", 0));
		AddBinding(m_CemeteryCapacity = new ValueBinding<int>("healthcareInfo", "cemeteryCapacity", 0));
		AddBinding(m_SickCount = new ValueBinding<int>("healthcareInfo", "sickCount", 0));
		AddBinding(m_PatientCount = new ValueBinding<int>("healthcareInfo", "patientCount", 0));
		AddBinding(m_PatientCapacity = new ValueBinding<int>("healthcareInfo", "patientCapacity", 0));
		AddBinding(m_HealthcareAvailability = new GetterValueBinding<IndicatorValue>("healthcareInfo", "healthcareAvailability", GetHealthcareAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_DeathcareAvailability = new GetterValueBinding<IndicatorValue>("healthcareInfo", "deathcareAvailability", GetDeathcareAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_CemeteryAvailability = new GetterValueBinding<IndicatorValue>("healthcareInfo", "cemeteryAvailability", GetCemeteryAvailability, new ValueWriter<IndicatorValue>()));
		m_Results = new NativeArray<float>(8, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		m_Results.Fill(0f);
		JobChunkExtensions.Schedule(new CalculateAverageHealthJob
		{
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizenType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblems = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_HouseholdQuery, base.Dependency).Complete();
		JobChunkExtensions.Schedule(new UpdateHealthcareJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PatientType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Patient_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HospitalDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HospitalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Result = m_Results
		}, m_HealthcareFacilityQuery, base.Dependency).Complete();
		JobChunkExtensions.Schedule(new UpdateDeathcareJob
		{
			m_DeathcareFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_DeathcareFacility_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_DeathcareFacilities = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_DeathcareFacilityQuery, base.Dependency).Complete();
		float num = m_Results[0];
		float x = m_Results[1];
		m_AverageHealth.Update(math.round(num / math.max(x, 1f)));
		m_PatientCount.Update((int)m_Results[3]);
		m_SickCount.Update((int)m_Results[2]);
		m_PatientCapacity.Update((int)m_Results[4]);
		m_DeathRate.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.DeathRate));
		m_ProcessingRate.Update(m_Results[5]);
		m_CemeteryUse.Update((int)m_Results[6]);
		m_CemeteryCapacity.Update((int)m_Results[7]);
		m_DeathcareAvailability.Update();
		m_CemeteryAvailability.Update();
		m_HealthcareAvailability.Update();
	}

	private IndicatorValue GetHealthcareAvailability()
	{
		return IndicatorValue.Calculate(m_PatientCapacity.value, m_SickCount.value);
	}

	private IndicatorValue GetDeathcareAvailability()
	{
		return IndicatorValue.Calculate(m_ProcessingRate.value, m_DeathRate.value);
	}

	private IndicatorValue GetCemeteryAvailability()
	{
		return IndicatorValue.Calculate(m_CemeteryCapacity.value, m_CemeteryUse.value);
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
	public HealthcareInfoviewUISystem()
	{
	}
}
