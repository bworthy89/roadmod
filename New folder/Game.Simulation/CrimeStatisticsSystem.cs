using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CrimeStatisticsSystem : GameSystemBase
{
	[BurstCompile]
	private struct AverageCrimeJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

		public float m_MaxCrimeAccumulation;

		public NativeAccumulator<AverageFloat>.ParallelWriter m_AverageCrime;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CrimeProducer> nativeArray = chunk.GetNativeArray(ref m_CrimeProducerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				m_AverageCrime.Accumulate(new AverageFloat
				{
					m_Count = 1,
					m_Total = nativeArray[i].m_Crime / m_MaxCrimeAccumulation
				});
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct StatisticsJob : IJob
	{
		public NativeAccumulator<AverageFloat> m_AverageCrime;

		public NativeQueue<StatisticsEvent> m_StatisticsEventQueue;

		public void Execute()
		{
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.CrimeRate,
				m_Change = 100f * m_AverageCrime.GetResult().average
			});
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeProducer>(isReadOnly: true);
		}
	}

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_CrimeProducerQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_263205583_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 8192;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CrimeProducerQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<CrimeProducer>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CrimeProducerQuery);
		RequireForUpdate<PoliceConfigurationData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PoliceConfigurationData singleton = __query_263205583_0.GetSingleton<PoliceConfigurationData>();
		if (!base.EntityManager.HasEnabledComponent<Locked>(singleton.m_PoliceServicePrefab))
		{
			NativeAccumulator<AverageFloat> averageCrime = new NativeAccumulator<AverageFloat>(Allocator.TempJob);
			AverageCrimeJob jobData = new AverageCrimeJob
			{
				m_CrimeProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MaxCrimeAccumulation = singleton.m_MaxCrimeAccumulation,
				m_AverageCrime = averageCrime.AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CrimeProducerQuery, base.Dependency);
			JobHandle deps;
			StatisticsJob jobData2 = new StatisticsJob
			{
				m_AverageCrime = averageCrime,
				m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps)
			};
			base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, deps));
			m_CityStatisticsSystem.AddWriter(base.Dependency);
			averageCrime.Dispose(base.Dependency);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<PoliceConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_263205583_0 = entityQueryBuilder2.Build(ref state);
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
	public CrimeStatisticsSystem()
	{
	}
}
