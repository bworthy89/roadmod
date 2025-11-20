using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
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
public class PoliceInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		CrimeProducerCount,
		CrimeProbability,
		ArrestedCriminals,
		JailCapacity,
		InJail,
		Prisoners,
		PrisonCapacity,
		InPrison,
		Count
	}

	[BurstCompile]
	private struct PoliceStationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public BufferTypeHandle<Occupant> m_OccupantHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> m_PoliceStationDataFromEntity;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgradesFromEntity;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			BufferAccessor<Occupant> bufferAccessor = chunk.GetBufferAccessor(ref m_OccupantHandle);
			BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = m_PrefabRefFromEntity[entity].m_Prefab;
				if (BuildingUtils.GetEfficiency(bufferAccessor2, i) != 0f)
				{
					m_PoliceStationDataFromEntity.TryGetComponent(prefab, out var componentData);
					if (m_InstalledUpgradesFromEntity.TryGetBuffer(entity, out var bufferData))
					{
						UpgradeUtils.CombineStats(ref componentData, bufferData, ref m_PrefabRefFromEntity, ref m_PoliceStationDataFromEntity);
					}
					m_Results[3] += componentData.m_JailCapacity;
					if (chunk.Has(ref m_OccupantHandle))
					{
						DynamicBuffer<Occupant> dynamicBuffer = bufferAccessor[i];
						m_Results[4] += dynamicBuffer.Length;
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
	private struct PrisonJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public BufferTypeHandle<Occupant> m_OccupantHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<PrisonData> m_PrisonDataFromEntity;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgradesFromEntity;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			BufferAccessor<Occupant> bufferAccessor = chunk.GetBufferAccessor(ref m_OccupantHandle);
			BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = m_PrefabRefFromEntity[entity].m_Prefab;
				if (BuildingUtils.GetEfficiency(bufferAccessor2, i) != 0f)
				{
					m_PrisonDataFromEntity.TryGetComponent(prefab, out var componentData);
					if (m_InstalledUpgradesFromEntity.TryGetBuffer(entity, out var bufferData))
					{
						UpgradeUtils.CombineStats(ref componentData, bufferData, ref m_PrefabRefFromEntity, ref m_PrisonDataFromEntity);
					}
					m_Results[6] += componentData.m_PrisonerCapacity;
					if (chunk.Has(ref m_OccupantHandle))
					{
						DynamicBuffer<Occupant> dynamicBuffer = bufferAccessor[i];
						m_Results[7] += dynamicBuffer.Length;
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
	private struct CrimeProducerJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerHandle;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CrimeProducer> nativeArray = chunk.GetNativeArray(ref m_CrimeProducerHandle);
			for (int i = 0; i < chunk.Count; i++)
			{
				CrimeProducer crimeProducer = nativeArray[i];
				m_Results[0] += 1f;
				m_Results[1] += crimeProducer.m_Crime;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CriminalJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Criminal> m_CriminalHandle;

		public NativeArray<float> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Criminal> nativeArray = chunk.GetNativeArray(ref m_CriminalHandle);
			for (int i = 0; i < chunk.Count; i++)
			{
				Criminal criminal = nativeArray[i];
				if ((criminal.m_Flags & (CriminalFlags.Prisoner | CriminalFlags.Sentenced)) != 0)
				{
					m_Results[5] += 1f;
				}
				else if ((criminal.m_Flags & CriminalFlags.Arrested) != 0)
				{
					m_Results[2] += 1f;
				}
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
		public ComponentTypeHandle<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Occupant> __Game_Buildings_Occupant_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> __Game_Prefabs_PoliceStationData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrisonData> __Game_Prefabs_PrisonData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Criminal> __Game_Citizens_Criminal_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeProducer>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Occupant_RO_BufferTypeHandle = state.GetBufferTypeHandle<Occupant>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PoliceStationData_RO_ComponentLookup = state.GetComponentLookup<PoliceStationData>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrisonData_RO_ComponentLookup = state.GetComponentLookup<PrisonData>(isReadOnly: true);
			__Game_Citizens_Criminal_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Criminal>(isReadOnly: true);
		}
	}

	private const string kGroup = "policeInfo";

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ValueBinding<int> m_CrimeProducers;

	private ValueBinding<float> m_CrimeProbability;

	private ValueBinding<int> m_JailCapacity;

	private ValueBinding<int> m_ArrestedCriminals;

	private ValueBinding<int> m_InJail;

	private ValueBinding<int> m_PrisonCapacity;

	private ValueBinding<int> m_Prisoners;

	private ValueBinding<int> m_InPrison;

	private ValueBinding<int> m_Criminals;

	private ValueBinding<int> m_CrimePerMonth;

	private ValueBinding<float> m_EscapedRate;

	private GetterValueBinding<IndicatorValue> m_AverageCrimeProbability;

	private GetterValueBinding<IndicatorValue> m_JailAvailability;

	private GetterValueBinding<IndicatorValue> m_PrisonAvailability;

	private EntityQuery m_PrisonQuery;

	private EntityQuery m_PrisonModifiedQuery;

	private EntityQuery m_CriminalQuery;

	private EntityQuery m_PoliceStationQuery;

	private EntityQuery m_PoliceStationModifiedQuery;

	private EntityQuery m_CrimeProducerQuery;

	private EntityQuery m_CrimeProducerModifiedQuery;

	private NativeArray<float> m_Results;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_632591896_0;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_AverageCrimeProbability.active && !m_JailAvailability.active && !m_PrisonAvailability.active && !m_CrimeProducers.active && !m_CrimeProbability.active && !m_PrisonCapacity.active && !m_Prisoners.active && !m_InPrison.active && !m_JailCapacity.active && !m_ArrestedCriminals.active)
			{
				return m_InJail.active;
			}
			return true;
		}
	}

	protected override bool Modified
	{
		get
		{
			if (m_CrimeProducerModifiedQuery.IsEmptyIgnoreFilter && m_PoliceStationModifiedQuery.IsEmptyIgnoreFilter)
			{
				return !m_PrisonModifiedQuery.IsEmptyIgnoreFilter;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_PrisonQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.Prison>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Owner>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PoliceStationQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Game.Buildings.PoliceStation>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Owner>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_CrimeProducerQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<CrimeProducer>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_CriminalQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<Criminal>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PrisonModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.Prison>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_PoliceStationModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.PoliceStation>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_CrimeProducerModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<CrimeProducer>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(m_CrimeProducers = new ValueBinding<int>("policeInfo", "crimeProducers", 0));
		AddBinding(m_CrimeProbability = new ValueBinding<float>("policeInfo", "crimeProbability", 0f));
		AddBinding(m_PrisonCapacity = new ValueBinding<int>("policeInfo", "prisonCapacity", 0));
		AddBinding(m_Prisoners = new ValueBinding<int>("policeInfo", "prisoners", 0));
		AddBinding(m_InPrison = new ValueBinding<int>("policeInfo", "inPrison", 0));
		AddBinding(m_JailCapacity = new ValueBinding<int>("policeInfo", "jailCapacity", 0));
		AddBinding(m_ArrestedCriminals = new ValueBinding<int>("policeInfo", "arrestedCriminals", 0));
		AddBinding(m_InJail = new ValueBinding<int>("policeInfo", "inJail", 0));
		AddBinding(m_Criminals = new ValueBinding<int>("policeInfo", "criminals", 0));
		AddBinding(m_CrimePerMonth = new ValueBinding<int>("policeInfo", "crimePerMonth", 0));
		AddBinding(m_EscapedRate = new ValueBinding<float>("policeInfo", "escapedRate", 0f));
		AddBinding(m_AverageCrimeProbability = new GetterValueBinding<IndicatorValue>("policeInfo", "averageCrimeProbability", GetCrimeProbability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_JailAvailability = new GetterValueBinding<IndicatorValue>("policeInfo", "jailAvailability", GetJailAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_PrisonAvailability = new GetterValueBinding<IndicatorValue>("policeInfo", "prisonAvailability", GetPrisonAvailability, new ValueWriter<IndicatorValue>()));
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
		ResetResults();
		JobChunkExtensions.Schedule(new CrimeProducerJob
		{
			m_CrimeProducerHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_CrimeProducerQuery, base.Dependency).Complete();
		JobChunkExtensions.Schedule(new PoliceStationJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OccupantHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Occupant_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceStationDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PoliceStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgradesFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_PoliceStationQuery, base.Dependency).Complete();
		JobChunkExtensions.Schedule(new PrisonJob
		{
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OccupantHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Occupant_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrisonDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrisonData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgradesFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_PrisonQuery, base.Dependency).Complete();
		JobChunkExtensions.Schedule(new CriminalJob
		{
			m_CriminalHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Criminal_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Results = m_Results
		}, m_CriminalQuery, base.Dependency).Complete();
		m_PrisonCapacity.Update((int)m_Results[6]);
		m_Prisoners.Update((int)m_Results[5]);
		m_InPrison.Update((int)m_Results[7]);
		m_JailCapacity.Update((int)m_Results[3]);
		m_ArrestedCriminals.Update((int)m_Results[2]);
		m_InJail.Update((int)m_Results[4]);
		m_CrimeProducers.Update((int)m_Results[0]);
		m_CrimeProbability.Update(m_Results[1]);
		m_Criminals.Update(m_CriminalQuery.CalculateEntityCount());
		m_AverageCrimeProbability.Update();
		m_JailAvailability.Update();
		m_PrisonAvailability.Update();
		m_CrimePerMonth.Update(m_CityStatisticsSystem.GetStatisticValue(StatisticType.CrimeCount));
		m_EscapedRate.Update((m_CrimePerMonth.value == 0) ? 0f : math.min(100f, (float)m_CityStatisticsSystem.GetStatisticValue(StatisticType.EscapedArrestCount) * 100f / (float)m_CrimePerMonth.value));
	}

	private void ResetResults()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			m_Results[i] = 0f;
		}
	}

	private IndicatorValue GetCrimeProbability()
	{
		float value = m_CrimeProbability.value;
		int value2 = m_CrimeProducers.value;
		return new IndicatorValue(0f, __query_632591896_0.GetSingleton<PoliceConfigurationData>().m_MaxCrimeAccumulation, (value2 > 0) ? (value / (float)value2) : 0f);
	}

	private IndicatorValue GetJailAvailability()
	{
		int value = m_JailCapacity.value;
		int value2 = m_ArrestedCriminals.value;
		return IndicatorValue.Calculate(value, value2, 0f);
	}

	private IndicatorValue GetPrisonAvailability()
	{
		int value = m_PrisonCapacity.value;
		int value2 = m_Prisoners.value;
		return IndicatorValue.Calculate(value, value2, 0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<PoliceConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_632591896_0 = entityQueryBuilder2.Build(ref state);
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
	public PoliceInfoviewUISystem()
	{
	}
}
