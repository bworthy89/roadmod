#define UNITY_ASSERTIONS
using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Prefabs;
using Game.Serialization;
using Game.Triggers;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CityStatisticsSystem : GameSystemBase, ICityStatisticsSystem, IDefaultSerializable, ISerializable, IPostDeserialize
{
	public readonly struct StatisticsKey : IEquatable<StatisticsKey>
	{
		public StatisticType type { get; }

		public int parameter { get; }

		public StatisticsKey(StatisticType type, int parameter)
		{
			this.type = type;
			this.parameter = parameter;
		}

		public override bool Equals(object obj)
		{
			if (obj is StatisticsKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(StatisticsKey other)
		{
			StatisticType num = type;
			int num2 = parameter;
			StatisticType statisticType = other.type;
			int num3 = other.parameter;
			if (num == statisticType)
			{
				return num2 == num3;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((int)type * 311 + parameter).GetHashCode();
		}
	}

	public struct SafeStatisticQueue
	{
		[NativeDisableContainerSafetyRestriction]
		private NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		[ReadOnly]
		public bool m_StatisticsEnabled;

		public SafeStatisticQueue(NativeQueue<StatisticsEvent> queue, bool enabled)
		{
			m_StatisticsEventQueue = queue.AsParallelWriter();
			m_StatisticsEnabled = enabled;
		}

		public void Enqueue(StatisticsEvent statisticsEvent)
		{
			if (m_StatisticsEnabled)
			{
				m_StatisticsEventQueue.Enqueue(statisticsEvent);
			}
		}
	}

	[BurstCompile]
	private struct CityStatisticsJob : IJob
	{
		public NativeQueue<StatisticsEvent> m_StatisticsEventQueue;

		[ReadOnly]
		public CountHouseholdDataSystem.HouseholdData m_HouseholdData;

		[ReadOnly]
		public ComponentLookup<Tourism> m_Tourisms;

		[ReadOnly]
		public Entity m_City;

		public float m_Money;

		public void Execute()
		{
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Money,
				m_Change = m_Money
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.LodgingTotal,
				m_Change = m_Tourisms[m_City].m_Lodging.y
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.LodgingUsed,
				m_Change = m_Tourisms[m_City].m_Lodging.x
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.HouseholdCount,
				m_Change = m_HouseholdData.m_MovedInHouseholdCount
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.TouristCount,
				m_Change = m_HouseholdData.m_TouristCitizenCount
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.AdultsCount,
				m_Change = m_HouseholdData.m_AdultCount
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.WorkerCount,
				m_Change = m_HouseholdData.m_CityWorkerCount
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Unemployed,
				m_Change = m_HouseholdData.Unemployed()
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Population,
				m_Change = m_HouseholdData.Population()
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Age,
				m_Change = m_HouseholdData.m_ChildrenCount,
				m_Parameter = 0
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Age,
				m_Change = m_HouseholdData.m_TeenCount,
				m_Parameter = 1
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Age,
				m_Change = m_HouseholdData.m_AdultCount,
				m_Parameter = 2
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Age,
				m_Change = m_HouseholdData.m_SeniorCount,
				m_Parameter = 3
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Wellbeing,
				m_Change = m_HouseholdData.m_TotalMovedInCitizenWellbeing
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.Health,
				m_Change = m_HouseholdData.m_TotalMovedInCitizenHealth
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.EducationCount,
				m_Change = m_HouseholdData.m_UneducatedCount,
				m_Parameter = 0
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.EducationCount,
				m_Change = m_HouseholdData.m_PoorlyEducatedCount,
				m_Parameter = 1
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.EducationCount,
				m_Change = m_HouseholdData.m_EducatedCount,
				m_Parameter = 2
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.EducationCount,
				m_Change = m_HouseholdData.m_WellEducatedCount,
				m_Parameter = 3
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.EducationCount,
				m_Change = m_HouseholdData.m_HighlyEducatedCount,
				m_Parameter = 4
			});
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.HomelessCount,
				m_Change = m_HouseholdData.m_HomelessCitizenCount
			});
		}
	}

	[BurstCompile]
	private struct ProcessStatisticsJob : IJob
	{
		public NativeQueue<StatisticsEvent> m_Queue;

		[ReadOnly]
		public NativeParallelHashMap<StatisticsKey, Entity> m_StatisticsLookup;

		public BufferLookup<CityStatistic> m_Statistics;

		public void Execute()
		{
			StatisticsEvent item;
			while (m_Queue.TryDequeue(out item))
			{
				if (item.m_Statistic == StatisticType.Count)
				{
					continue;
				}
				StatisticsKey key = new StatisticsKey(item.m_Statistic, item.m_Parameter);
				if (!m_StatisticsLookup.ContainsKey(key))
				{
					continue;
				}
				Entity entity = m_StatisticsLookup[key];
				if (m_Statistics.HasBuffer(entity))
				{
					DynamicBuffer<CityStatistic> dynamicBuffer = m_Statistics[entity];
					if (dynamicBuffer.Length == 0)
					{
						dynamicBuffer.Add(new CityStatistic
						{
							m_TotalValue = 0.0,
							m_Value = 0.0
						});
					}
					CityStatistic value = dynamicBuffer[dynamicBuffer.Length - 1];
					if (dynamicBuffer.Length == 1 && item.m_Statistic == StatisticType.Money)
					{
						value.m_TotalValue = item.m_Change;
					}
					value.m_Value += item.m_Change;
					dynamicBuffer[dynamicBuffer.Length - 1] = value;
				}
			}
		}
	}

	[BurstCompile]
	private struct ResetEntityJob : IJob
	{
		public int m_Money;

		[ReadOnly]
		public NativeParallelHashMap<StatisticsKey, Entity> m_StatisticsLookup;

		public BufferLookup<CityStatistic> m_Statistics;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<StatisticsData> m_PrefabStats;

		public void Execute()
		{
			NativeArray<StatisticsKey> keyArray = m_StatisticsLookup.GetKeyArray(Allocator.Temp);
			for (int i = 0; i < keyArray.Length; i++)
			{
				Entity entity = m_StatisticsLookup[keyArray[i]];
				if (!m_Statistics.HasBuffer(entity))
				{
					continue;
				}
				DynamicBuffer<CityStatistic> dynamicBuffer = m_Statistics[entity];
				Entity prefab = m_Prefabs[entity].m_Prefab;
				StatisticsData statisticsData = m_PrefabStats[prefab];
				if (dynamicBuffer.Length == 0)
				{
					dynamicBuffer.Add(new CityStatistic
					{
						m_TotalValue = 0.0,
						m_Value = ((statisticsData.m_StatisticType == StatisticType.Money) ? m_Money : 0)
					});
				}
				CityStatistic cityStatistic = dynamicBuffer[dynamicBuffer.Length - 1];
				if (statisticsData.m_CollectionType == StatisticCollectionType.Cumulative)
				{
					dynamicBuffer.Add(new CityStatistic
					{
						m_TotalValue = cityStatistic.m_TotalValue + cityStatistic.m_Value,
						m_Value = 0.0
					});
				}
				else if (statisticsData.m_CollectionType == StatisticCollectionType.Point)
				{
					dynamicBuffer.Add(new CityStatistic
					{
						m_TotalValue = cityStatistic.m_Value,
						m_Value = 0.0
					});
				}
				else if (statisticsData.m_CollectionType == StatisticCollectionType.Daily)
				{
					double num = 0.0;
					if (dynamicBuffer.Length >= 32)
					{
						num = dynamicBuffer[dynamicBuffer.Length - 32].m_Value;
					}
					dynamicBuffer.Add(new CityStatistic
					{
						m_TotalValue = cityStatistic.m_TotalValue + cityStatistic.m_Value - num,
						m_Value = 0.0
					});
				}
			}
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<Tourism> __Game_City_Tourism_RW_ComponentLookup;

		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StatisticsData> __Game_Prefabs_StatisticsData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_Tourism_RW_ComponentLookup = state.GetComponentLookup<Tourism>();
			__Game_City_CityStatistic_RW_BufferLookup = state.GetBufferLookup<CityStatistic>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_StatisticsData_RO_ComponentLookup = state.GetComponentLookup<StatisticsData>(isReadOnly: true);
		}
	}

	public const int kUpdatesPerDay = 32;

	private CitySystem m_CitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private TriggerSystem m_TriggerSystem;

	private EntityQuery m_StatisticsPrefabQuery;

	private EntityQuery m_StatisticsQuery;

	private EntityQuery m_CityQuery;

	private NativeParallelHashMap<StatisticsKey, Entity> m_StatisticsLookup;

	private NativeQueue<StatisticsEvent> m_StatisticsEventQueue;

	private JobHandle m_Writers;

	private bool m_Initialized;

	private int m_SampleCount = 1;

	private uint m_LastSampleFrameIndex;

	private TypeHandle __TypeHandle;

	public int sampleCount => m_SampleCount;

	public Action eventStatisticsUpdated { get; set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 8192;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 0;
	}

	public NativeParallelHashMap<StatisticsKey, Entity> GetLookup()
	{
		return m_StatisticsLookup;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_StatisticsPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<StatisticsData>());
		m_StatisticsQuery = GetEntityQuery(ComponentType.ReadOnly<CityStatistic>());
		m_CityQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_CityQuery);
		m_StatisticsLookup = new NativeParallelHashMap<StatisticsKey, Entity>(64, Allocator.Persistent);
		m_StatisticsEventQueue = new NativeQueue<StatisticsEvent>(Allocator.Persistent);
		base.Enabled = false;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_StatisticsLookup.Dispose();
		m_StatisticsEventQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_Initialized)
		{
			InitializeLookup();
		}
		JobHandle job = IJobExtensions.Schedule(new CityStatisticsJob
		{
			m_StatisticsEventQueue = m_StatisticsEventQueue,
			m_HouseholdData = m_CountHouseholdDataSystem.GetHouseholdCountData(),
			m_Tourisms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Tourism_RW_ComponentLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_Money = m_CitySystem.moneyAmount
		}, JobHandle.CombineDependencies(base.Dependency, m_Writers));
		JobHandle dependsOn = IJobExtensions.Schedule(new ProcessStatisticsJob
		{
			m_Statistics = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RW_BufferLookup, ref base.CheckedStateRef),
			m_StatisticsLookup = m_StatisticsLookup,
			m_Queue = m_StatisticsEventQueue
		}, JobHandle.CombineDependencies(job, base.Dependency));
		JobHandle dependency = IJobExtensions.Schedule(new ResetEntityJob
		{
			m_Money = m_CitySystem.moneyAmount,
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStats = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StatisticsData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Statistics = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RW_BufferLookup, ref base.CheckedStateRef),
			m_StatisticsLookup = m_StatisticsLookup
		}, dependsOn);
		base.Dependency = dependency;
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		AddWriter(base.Dependency);
		m_SampleCount++;
		m_LastSampleFrameIndex = m_SimulationSystem.frameIndex;
		eventStatisticsUpdated?.Invoke();
	}

	public static int GetStatisticValue(NativeParallelHashMap<StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		long statisticValueLong = GetStatisticValueLong(statisticsLookup, stats, type, parameter);
		if (statisticValueLong <= int.MaxValue)
		{
			if (statisticValueLong >= int.MinValue)
			{
				return (int)statisticValueLong;
			}
			return int.MinValue;
		}
		return int.MaxValue;
	}

	public static long GetStatisticValueLong(NativeParallelHashMap<StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		double statisticValueDouble = GetStatisticValueDouble(statisticsLookup, stats, type, parameter);
		if (!(statisticValueDouble > 9.223372036854776E+18))
		{
			if (!(statisticValueDouble < -9.223372036854776E+18))
			{
				return (long)statisticValueDouble;
			}
			return long.MinValue;
		}
		return long.MaxValue;
	}

	private static double GetStatisticValueDouble(NativeParallelHashMap<StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		StatisticsKey key = new StatisticsKey(type, parameter);
		if (statisticsLookup.ContainsKey(key))
		{
			Entity entity = statisticsLookup[key];
			if (stats.HasBuffer(entity))
			{
				DynamicBuffer<CityStatistic> dynamicBuffer = stats[entity];
				if (dynamicBuffer.Length > 0)
				{
					return Math.Round(dynamicBuffer[dynamicBuffer.Length - 1].m_TotalValue, MidpointRounding.AwayFromZero);
				}
				return 0.0;
			}
		}
		return 0.0;
	}

	public int GetStatisticValue(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		long statisticValueLong = GetStatisticValueLong(m_StatisticsLookup, stats, type, parameter);
		if (statisticValueLong <= int.MaxValue)
		{
			if (statisticValueLong >= int.MinValue)
			{
				return (int)statisticValueLong;
			}
			return int.MinValue;
		}
		return int.MaxValue;
	}

	public long GetStatisticValueLong(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		double statisticValueDouble = GetStatisticValueDouble(m_StatisticsLookup, stats, type, parameter);
		if (!(statisticValueDouble > 9.223372036854776E+18))
		{
			if (!(statisticValueDouble < -9.223372036854776E+18))
			{
				return (long)statisticValueDouble;
			}
			return long.MinValue;
		}
		return long.MaxValue;
	}

	private double GetStatisticValueDouble(StatisticType type, int parameter = 0)
	{
		StatisticsKey key = new StatisticsKey(type, parameter);
		m_Writers.Complete();
		if (m_StatisticsLookup.ContainsKey(key))
		{
			Entity entity = m_StatisticsLookup[key];
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CityStatistic> buffer) && buffer.Length > 0)
			{
				return Math.Round(buffer[buffer.Length - 1].m_TotalValue, MidpointRounding.AwayFromZero);
			}
		}
		return 0.0;
	}

	public int GetStatisticValue(StatisticType type, int parameter = 0)
	{
		long statisticValueLong = GetStatisticValueLong(type, parameter);
		if (statisticValueLong <= int.MaxValue)
		{
			if (statisticValueLong >= int.MinValue)
			{
				return (int)statisticValueLong;
			}
			return int.MinValue;
		}
		return int.MaxValue;
	}

	public long GetStatisticValueLong(StatisticType type, int parameter = 0)
	{
		double statisticValueDouble = GetStatisticValueDouble(type, parameter);
		if (!(statisticValueDouble > 9.223372036854776E+18))
		{
			if (!(statisticValueDouble < -9.223372036854776E+18))
			{
				return (long)statisticValueDouble;
			}
			return long.MinValue;
		}
		return long.MaxValue;
	}

	public static NativeArray<long> GetStatisticDataArrayLong(NativeParallelHashMap<StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		StatisticsKey key = new StatisticsKey(type, parameter);
		if (statisticsLookup.ContainsKey(key))
		{
			Entity entity = statisticsLookup[key];
			if (stats.HasBuffer(entity))
			{
				DynamicBuffer<CityStatistic> dynamicBuffer = stats[entity];
				NativeArray<long> result = new NativeArray<long>(dynamicBuffer.Length, Allocator.Temp);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					double num = Math.Round(dynamicBuffer[i].m_TotalValue, MidpointRounding.AwayFromZero);
					result[i] = ((num > 9.223372036854776E+18) ? long.MaxValue : ((num < -9.223372036854776E+18) ? long.MinValue : ((long)num)));
				}
				return result;
			}
		}
		return new NativeArray<long>(1, Allocator.Temp);
	}

	public static NativeArray<int> GetStatisticDataArray(NativeParallelHashMap<StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		StatisticsKey key = new StatisticsKey(type, parameter);
		if (statisticsLookup.ContainsKey(key))
		{
			Entity entity = statisticsLookup[key];
			if (stats.HasBuffer(entity))
			{
				DynamicBuffer<CityStatistic> dynamicBuffer = stats[entity];
				NativeArray<int> result = new NativeArray<int>(dynamicBuffer.Length, Allocator.Temp);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					double num = Math.Round(dynamicBuffer[i].m_TotalValue, MidpointRounding.AwayFromZero);
					result[i] = ((num > 2147483647.0) ? int.MaxValue : ((num < -2147483648.0) ? int.MinValue : ((int)num)));
				}
				return result;
			}
		}
		return new NativeArray<int>(1, Allocator.Temp);
	}

	public NativeArray<long> GetStatisticDataArrayLong(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		return GetStatisticDataArrayLong(m_StatisticsLookup, stats, type, parameter);
	}

	public NativeArray<int> GetStatisticDataArray(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0)
	{
		return GetStatisticDataArray(m_StatisticsLookup, stats, type, parameter);
	}

	public NativeArray<long> GetStatisticDataArrayLong(StatisticType type, int parameter = 0)
	{
		StatisticsKey key = new StatisticsKey(type, parameter);
		if (m_StatisticsLookup.ContainsKey(key))
		{
			Entity entity = m_StatisticsLookup[key];
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CityStatistic> buffer))
			{
				NativeArray<long> result = new NativeArray<long>(buffer.Length, Allocator.Temp);
				for (int i = 0; i < buffer.Length; i++)
				{
					double num = Math.Round(buffer[i].m_TotalValue, MidpointRounding.AwayFromZero);
					result[i] = ((num > 9.223372036854776E+18) ? long.MaxValue : ((num < -9.223372036854776E+18) ? long.MinValue : ((long)num)));
				}
				return result;
			}
		}
		return new NativeArray<long>(1, Allocator.Temp);
	}

	public NativeArray<int> GetStatisticDataArray(StatisticType type, int parameter = 0)
	{
		StatisticsKey key = new StatisticsKey(type, parameter);
		if (m_StatisticsLookup.ContainsKey(key))
		{
			Entity entity = m_StatisticsLookup[key];
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CityStatistic> buffer))
			{
				NativeArray<int> result = new NativeArray<int>(buffer.Length, Allocator.Temp);
				for (int i = 0; i < buffer.Length; i++)
				{
					double num = Math.Round(buffer[i].m_TotalValue, MidpointRounding.AwayFromZero);
					result[i] = ((num > 2147483647.0) ? int.MaxValue : ((num < -2147483648.0) ? int.MinValue : ((int)num)));
				}
				return result;
			}
		}
		return new NativeArray<int>(1, Allocator.Temp);
	}

	public NativeArray<CityStatistic> GetStatisticArray(StatisticType type, int parameter = 0)
	{
		StatisticsKey key = new StatisticsKey(type, parameter);
		m_Writers.Complete();
		if (m_StatisticsLookup.ContainsKey(key))
		{
			Entity entity = m_StatisticsLookup[key];
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CityStatistic> buffer))
			{
				return buffer.AsNativeArray();
			}
		}
		return new NativeArray<CityStatistic>(1, Allocator.Temp);
	}

	public uint GetSampleFrameIndex(int index)
	{
		int num = (sampleCount - index - 1) * 8192;
		return m_LastSampleFrameIndex - (uint)num;
	}

	private void InitializeLookup()
	{
		m_StatisticsLookup.Clear();
		NativeArray<Entity> nativeArray = m_StatisticsPrefabQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<StatisticsData> nativeArray2 = m_StatisticsPrefabQuery.ToComponentDataArray<StatisticsData>(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			StatisticType statisticType = nativeArray2[i].m_StatisticType;
			if (base.EntityManager.TryGetBuffer(nativeArray[i], isReadOnly: true, out DynamicBuffer<StatisticParameterData> buffer))
			{
				for (int j = 0; j < buffer.Length; j++)
				{
					m_StatisticsLookup.Add(new StatisticsKey(statisticType, buffer[j].m_Value), Entity.Null);
				}
			}
			else
			{
				m_StatisticsLookup.Add(new StatisticsKey(statisticType, 0), Entity.Null);
			}
		}
		NativeArray<Entity> nativeArray3 = m_StatisticsQuery.ToEntityArray(Allocator.TempJob);
		bool flag = true;
		for (int k = 0; k < nativeArray3.Length; k++)
		{
			if (!base.EntityManager.TryGetComponent<PrefabRef>(nativeArray3[k], out var component))
			{
				continue;
			}
			int num = 0;
			if (base.EntityManager.TryGetComponent<StatisticParameter>(nativeArray3[k], out var component2))
			{
				num = component2.m_Value;
			}
			if (!base.EntityManager.TryGetBuffer(component.m_Prefab, isReadOnly: true, out DynamicBuffer<StatisticParameterData> buffer2))
			{
				continue;
			}
			flag = false;
			for (int l = 0; l < buffer2.Length; l++)
			{
				if (num == buffer2[l].m_Value)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		if (flag)
		{
			for (int m = 0; m < nativeArray3.Length; m++)
			{
				if (base.EntityManager.TryGetComponent<PrefabRef>(nativeArray3[m], out var component3) && base.EntityManager.TryGetComponent<StatisticsData>(component3.m_Prefab, out var component4))
				{
					int parameter = 0;
					if (base.EntityManager.TryGetComponent<StatisticParameter>(nativeArray3[m], out var component5))
					{
						parameter = component5.m_Value;
					}
					StatisticsKey key = new StatisticsKey(component4.m_StatisticType, parameter);
					if (m_StatisticsLookup.ContainsKey(key))
					{
						m_StatisticsLookup[key] = nativeArray3[m];
					}
				}
			}
		}
		else
		{
			base.EntityManager.DestroyEntity(m_StatisticsQuery);
			m_SampleCount = 0;
			m_StatisticsEventQueue.Clear();
		}
		nativeArray3.Dispose();
		NativeKeyValueArrays<StatisticsKey, Entity> keyValueArrays = m_StatisticsLookup.GetKeyValueArrays(Allocator.Temp);
		for (int n = 0; n < keyValueArrays.Length; n++)
		{
			if (!(keyValueArrays.Values[n] == Entity.Null))
			{
				continue;
			}
			StatisticsKey key2 = keyValueArrays.Keys[n];
			StatisticType type = key2.type;
			for (int num2 = 0; num2 < nativeArray.Length; num2++)
			{
				Entity entity = nativeArray[num2];
				DynamicBuffer<StatisticParameterData> buffer3;
				bool flag2 = base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out buffer3);
				if (!base.EntityManager.TryGetComponent<StatisticsData>(entity, out var component6) || component6.m_StatisticType != type)
				{
					continue;
				}
				if (flag2)
				{
					bool flag3 = false;
					for (int num3 = 0; num3 < buffer3.Length; num3++)
					{
						if (buffer3[num3].m_Value == key2.parameter)
						{
							flag3 = true;
						}
					}
					if (!flag3)
					{
						continue;
					}
				}
				ArchetypeData componentData = base.EntityManager.GetComponentData<ArchetypeData>(entity);
				m_StatisticsLookup[key2] = StatisticsPrefab.CreateInstance(base.World.EntityManager, entity, componentData, key2.parameter);
				break;
			}
		}
		m_Initialized = true;
		nativeArray.Dispose();
		nativeArray2.Dispose();
	}

	public void CompleteWriters()
	{
		m_Writers.Complete();
	}

	public NativeQueue<StatisticsEvent> GetStatisticsEventQueue(out JobHandle deps)
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		deps = m_Writers;
		return m_StatisticsEventQueue;
	}

	public SafeStatisticQueue GetSafeStatisticsQueue(out JobHandle deps)
	{
		deps = m_Writers;
		return new SafeStatisticQueue(m_StatisticsEventQueue, base.Enabled);
	}

	public void AddWriter(JobHandle writer)
	{
		m_Writers = JobHandle.CombineDependencies(m_Writers, writer);
	}

	public void DiscardStatistics()
	{
		m_Writers.Complete();
		m_StatisticsEventQueue.Clear();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		m_Writers.Complete();
		int value = m_SampleCount;
		writer.Write(value);
		uint value2 = m_LastSampleFrameIndex;
		writer.Write(value2);
		int count = m_StatisticsEventQueue.Count;
		writer.Write(count);
		NativeArray<StatisticsEvent> nativeArray = m_StatisticsEventQueue.ToArray(Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			StatisticsEvent value3 = nativeArray[i];
			writer.Write(value3);
		}
		nativeArray.Dispose();
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_Writers.Complete();
		if (reader.context.version >= Version.statisticsRefactor)
		{
			ref int value = ref m_SampleCount;
			reader.Read(out value);
			if (reader.context.version >= Version.statsLastFrameIndex)
			{
				ref uint value2 = ref m_LastSampleFrameIndex;
				reader.Read(out value2);
			}
			if (!(reader.context.version >= Version.statisticsFix2))
			{
				return;
			}
			m_StatisticsEventQueue.Clear();
			reader.Read(out int value3);
			for (int i = 0; i < value3; i++)
			{
				reader.Read(out StatisticsEvent value4);
				if (!(reader.context.version < Version.statisticUnifying) || (value4.m_Statistic != StatisticType.Population && value4.m_Statistic != StatisticType.Health && value4.m_Statistic != StatisticType.Age && value4.m_Statistic != StatisticType.Wellbeing && value4.m_Statistic != StatisticType.AdultsCount && value4.m_Statistic != StatisticType.HouseholdCount && value4.m_Statistic != StatisticType.EducationCount && value4.m_Statistic != StatisticType.Unemployed && value4.m_Statistic != StatisticType.WorkerCount))
				{
					m_StatisticsEventQueue.Enqueue(value4);
				}
			}
		}
		else
		{
			reader.Read(out Entity value5);
			reader.Read(out value5);
			reader.Read(out value5);
			reader.Read(out int value6);
			int value7;
			for (int j = 0; j < value6; j++)
			{
				reader.Read(out value7);
				reader.Read(out value7);
			}
			reader.Read(out value6);
			NativeArray<Entity> nativeArray = new NativeArray<Entity>(value6, Allocator.Temp);
			NativeArray<Entity> value8 = nativeArray;
			reader.Read(value8);
			nativeArray.Dispose();
			reader.Read(out value7);
			reader.Read(out value7);
		}
	}

	public void SetDefaults(Context context)
	{
		m_SampleCount = 0;
		m_StatisticsEventQueue.Clear();
	}

	public void PostDeserialize(Context context)
	{
		m_StatisticsLookup.Clear();
		InitializeLookup();
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
	public CityStatisticsSystem()
	{
	}
}
