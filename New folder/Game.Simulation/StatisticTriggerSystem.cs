using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.City;
using Game.Prefabs;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class StatisticTriggerSystem : GameSystemBase
{
	[BurstCompile]
	private struct SendTriggersJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StatisticTriggerData> m_StatisticTriggerDataHandle;

		[ReadOnly]
		public ComponentLookup<StatisticsData> m_StatisticsDatas;

		[ReadOnly]
		public ComponentLookup<Locked> m_Locked;

		[ReadOnly]
		public BufferLookup<CityStatistic> m_CityStatistics;

		[ReadOnly]
		public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_StatisticsLookup;

		public NativeQueue<TriggerAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityTypeHandle);
			NativeArray<StatisticTriggerData> nativeArray2 = chunk.GetNativeArray(ref m_StatisticTriggerDataHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				StatisticTriggerData statisticTriggerData = nativeArray2[i];
				NativeArray<int> statisticDataArray = CityStatisticsSystem.GetStatisticDataArray(m_StatisticsLookup, m_CityStatistics, m_StatisticsDatas[statisticTriggerData.m_StatisticEntity].m_StatisticType, statisticTriggerData.m_StatisticParameter);
				int num = math.max(1, statisticTriggerData.m_TimeFrame);
				TriggerAction value = new TriggerAction
				{
					m_TriggerType = TriggerType.StatisticsValue,
					m_TriggerPrefab = nativeArray[i]
				};
				if (statisticTriggerData.m_NormalizeWithPrefab != Entity.Null)
				{
					NativeArray<int> statisticDataArray2 = CityStatisticsSystem.GetStatisticDataArray(m_StatisticsLookup, m_CityStatistics, m_StatisticsDatas[statisticTriggerData.m_NormalizeWithPrefab].m_StatisticType, statisticTriggerData.m_NormalizeWithParameter);
					if (statisticDataArray.Length < num || statisticDataArray2.Length < num || statisticDataArray.Length < statisticTriggerData.m_MinSamples || statisticDataArray2.Length < statisticTriggerData.m_MinSamples || m_Locked.HasEnabledComponent(statisticTriggerData.m_StatisticEntity) || m_Locked.HasEnabledComponent(statisticTriggerData.m_NormalizeWithPrefab))
					{
						continue;
					}
					if (statisticTriggerData.m_Type == StatisticTriggerType.TotalValue)
					{
						if (NonZeroValues(statisticDataArray2, num))
						{
							float num2 = 0f;
							for (int j = 1; j <= num; j++)
							{
								num2 += (float)statisticDataArray[statisticDataArray.Length - j] / (float)statisticDataArray2[statisticDataArray2.Length - j];
							}
							value.m_Value = num2;
							m_ActionQueue.Enqueue(value);
						}
					}
					else if (statisticTriggerData.m_Type == StatisticTriggerType.AverageValue)
					{
						if (NonZeroValues(statisticDataArray2, num))
						{
							float num3 = 0f;
							for (int k = 1; k <= num; k++)
							{
								num3 += (float)statisticDataArray[statisticDataArray.Length - k] / (float)statisticDataArray2[statisticDataArray2.Length - k];
							}
							value.m_Value = num3 / (float)num;
							m_ActionQueue.Enqueue(value);
						}
					}
					else if (statisticTriggerData.m_Type == StatisticTriggerType.AbsoluteChange)
					{
						if (statisticDataArray2[statisticDataArray2.Length - num] != 0 && statisticDataArray2[statisticDataArray2.Length - 1] != 0)
						{
							float num4 = (float)statisticDataArray[statisticDataArray.Length - num] / (float)statisticDataArray2[statisticDataArray2.Length - num];
							float num5 = (float)statisticDataArray[statisticDataArray.Length - 1] / (float)statisticDataArray2[statisticDataArray2.Length - 1];
							value.m_Value = num5 - num4;
							m_ActionQueue.Enqueue(value);
						}
					}
					else if (statisticTriggerData.m_Type == StatisticTriggerType.RelativeChange && statisticDataArray2[statisticDataArray2.Length - num] != 0 && statisticDataArray2[statisticDataArray2.Length - 1] != 0)
					{
						float num6 = statisticDataArray[statisticDataArray.Length - num] / statisticDataArray2[statisticDataArray2.Length - num];
						float num7 = statisticDataArray[statisticDataArray.Length - 1] / statisticDataArray2[statisticDataArray2.Length - 1];
						if (num6 != 0f)
						{
							value.m_Value = (num7 - num6) / num6;
							m_ActionQueue.Enqueue(value);
						}
					}
				}
				else
				{
					if (statisticDataArray.Length < num || statisticDataArray.Length < statisticTriggerData.m_MinSamples || m_Locked.HasEnabledComponent(statisticTriggerData.m_StatisticEntity))
					{
						continue;
					}
					if (statisticTriggerData.m_Type == StatisticTriggerType.TotalValue)
					{
						float num8 = 0f;
						for (int l = 1; l <= num; l++)
						{
							num8 += (float)statisticDataArray[statisticDataArray.Length - l];
						}
						value.m_Value = num8;
						m_ActionQueue.Enqueue(value);
					}
					else if (statisticTriggerData.m_Type == StatisticTriggerType.AverageValue)
					{
						float num9 = 0f;
						for (int m = 1; m <= num; m++)
						{
							num9 += (float)statisticDataArray[statisticDataArray.Length - m];
						}
						value.m_Value = num9 / (float)num;
						m_ActionQueue.Enqueue(value);
					}
					else if (statisticTriggerData.m_Type == StatisticTriggerType.AbsoluteChange)
					{
						float num10 = statisticDataArray[statisticDataArray.Length - num];
						float num11 = statisticDataArray[statisticDataArray.Length - 1];
						value.m_Value = num11 - num10;
						m_ActionQueue.Enqueue(value);
					}
					else if (statisticTriggerData.m_Type == StatisticTriggerType.RelativeChange)
					{
						float num12 = statisticDataArray[statisticDataArray.Length - num];
						float num13 = statisticDataArray[statisticDataArray.Length - 1];
						if (num12 != 0f)
						{
							value.m_Value = (num13 - num12) / num12;
							m_ActionQueue.Enqueue(value);
						}
					}
				}
			}
		}

		private bool NonZeroValues(NativeArray<int> values, int timeframe)
		{
			for (int i = 1; i <= timeframe; i++)
			{
				if (values[values.Length - i] == 0)
				{
					return false;
				}
			}
			return true;
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

		[ReadOnly]
		public ComponentTypeHandle<StatisticTriggerData> __Game_Prefabs_StatisticTriggerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<StatisticsData> __Game_Prefabs_StatisticsData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_StatisticTriggerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StatisticTriggerData>(isReadOnly: true);
			__Game_Prefabs_StatisticsData_RO_ComponentLookup = state.GetComponentLookup<StatisticsData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(isReadOnly: true);
		}
	}

	public const int kUpdatesPerDay = 32;

	private ICityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_PrefabQuery;

	private TriggerSystem m_TriggerSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 8192;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 0;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<StatisticTriggerData>(), ComponentType.ReadOnly<TriggerData>());
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		SendTriggersJob jobData = new SendTriggersJob
		{
			m_EntityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_StatisticTriggerDataHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_StatisticTriggerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StatisticsDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StatisticsData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Locked = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityStatistics = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef),
			m_StatisticsLookup = m_CityStatisticsSystem.GetLookup(),
			m_ActionQueue = m_TriggerSystem.CreateActionBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_PrefabQuery, base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
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
	public StatisticTriggerSystem()
	{
	}
}
