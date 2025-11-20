using System;
using System.Runtime.CompilerServices;
using Game.City;
using Game.Economy;
using Game.Prefabs;
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
public class XPAccumulationSystem : GameSystemBase
{
	[BurstCompile]
	private struct XPAccumulateJob : IJob
	{
		[ReadOnly]
		public XPParameterData m_XPParameters;

		[ReadOnly]
		public ComponentLookup<Population> m_CityPopulations;

		[ReadOnly]
		public BufferLookup<CityStatistic> m_CityStatistics;

		[ReadOnly]
		public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_StatsLookup;

		public ComponentLookup<XP> m_CityXPs;

		public NativeQueue<XPGain> m_XPQueue;

		[ReadOnly]
		public Entity m_City;

		public void Execute()
		{
			Population population = m_CityPopulations[m_City];
			if (population.m_Population >= 10)
			{
				XP value = m_CityXPs[m_City];
				int num = math.max(0, population.m_Population - value.m_MaximumPopulation);
				value.m_MaximumPopulation = Math.Max(value.m_MaximumPopulation, population.m_Population);
				int num2 = 0;
				for (int i = 0; i < 5; i++)
				{
					num2 = CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.ResidentialTaxableIncome, i);
				}
				ResourceIterator iterator = ResourceIterator.GetIterator();
				while (iterator.Next())
				{
					num2 += CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.CommercialTaxableIncome, (int)iterator.resource) + CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.IndustrialTaxableIncome, (int)iterator.resource) + CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.OfficeTaxableIncome, (int)iterator.resource);
				}
				int val = num2 / 10;
				value.m_MaximumIncome = Math.Max(value.m_MaximumIncome, val);
				m_CityXPs[m_City] = value;
				m_XPQueue.Enqueue(new XPGain
				{
					amount = Mathf.FloorToInt(m_XPParameters.m_XPPerPopulation * (float)num / (float)kUpdatesPerDay),
					entity = Entity.Null,
					reason = XPReason.Population
				});
				m_XPQueue.Enqueue(new XPGain
				{
					amount = Mathf.FloorToInt(m_XPParameters.m_XPPerHappiness * (float)population.m_AverageHappiness / (float)kUpdatesPerDay),
					entity = Entity.Null,
					reason = XPReason.Happiness
				});
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		public ComponentLookup<XP> __Game_City_XP_RW_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_City_XP_RW_ComponentLookup = state.GetComponentLookup<XP>();
			__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private XPSystem m_XPSystem;

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_XPSettingsQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_XPSystem = base.World.GetOrCreateSystemManaged<XPSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_XPSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<XPParameterData>());
		RequireForUpdate(m_XPSettingsQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		XPAccumulateJob jobData = new XPAccumulateJob
		{
			m_CityPopulations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityXPs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_XP_RW_ComponentLookup, ref base.CheckedStateRef),
			m_XPParameters = m_XPSettingsQuery.GetSingleton<XPParameterData>(),
			m_CityStatistics = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef),
			m_StatsLookup = m_CityStatisticsSystem.GetLookup(),
			m_City = m_CitySystem.City,
			m_XPQueue = m_XPSystem.GetQueue(out deps)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, deps));
		m_XPSystem.AddQueueWriter(base.Dependency);
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
	public XPAccumulationSystem()
	{
	}
}
