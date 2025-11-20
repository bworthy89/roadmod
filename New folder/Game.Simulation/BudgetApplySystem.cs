using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.City;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BudgetApplySystem : GameSystemBase
{
	[BurstCompile]
	private struct BudgetApplyJob : IJob
	{
		public NativeArray<int> m_Income;

		public NativeArray<int> m_Expenses;

		public ComponentLookup<PlayerMoney> m_PlayerMoneys;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		public Entity m_City;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < 15; i++)
			{
				ExpenseSource parameter = (ExpenseSource)i;
				int expense = CityServiceBudgetSystem.GetExpense((ExpenseSource)i, m_Expenses);
				num -= expense;
				m_StatisticsEventQueue.Enqueue(new StatisticsEvent
				{
					m_Statistic = StatisticType.Expense,
					m_Change = math.abs((float)expense / (float)kUpdatesPerDay),
					m_Parameter = (int)parameter
				});
			}
			for (int j = 0; j < 14; j++)
			{
				IncomeSource parameter2 = (IncomeSource)j;
				int income = CityServiceBudgetSystem.GetIncome((IncomeSource)j, m_Income);
				num += income;
				m_StatisticsEventQueue.Enqueue(new StatisticsEvent
				{
					m_Statistic = StatisticType.Income,
					m_Change = math.abs((float)income / (float)kUpdatesPerDay),
					m_Parameter = (int)parameter2
				});
			}
			PlayerMoney value = m_PlayerMoneys[m_City];
			value.Add(num / kUpdatesPerDay);
			m_PlayerMoneys[m_City] = value;
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_PlayerMoney_RW_ComponentLookup = state.GetComponentLookup<PlayerMoney>();
		}
	}

	public static readonly int kUpdatesPerDay = 1024;

	private CitySystem m_CitySystem;

	private CityServiceBudgetSystem m_CityServiceBudgetSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityServiceBudgetSystem = base.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		JobHandle deps2;
		JobHandle deps3;
		BudgetApplyJob jobData = new BudgetApplyJob
		{
			m_PlayerMoneys = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_PlayerMoney_RW_ComponentLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_Expenses = m_CityServiceBudgetSystem.GetExpenseArray(out deps),
			m_Income = m_CityServiceBudgetSystem.GetIncomeArray(out deps2),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps3).AsParallelWriter()
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(deps, deps2, deps3, base.Dependency));
		m_CityServiceBudgetSystem.AddArrayReader(base.Dependency);
		m_CityStatisticsSystem.AddWriter(base.Dependency);
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
	public BudgetApplySystem()
	{
	}
}
