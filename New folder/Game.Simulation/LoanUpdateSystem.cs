using System.Runtime.CompilerServices;
using Game.City;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class LoanUpdateSystem : GameSystemBase
{
	[BurstCompile]
	private struct LoanUpdateJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<Loan> m_Loans;

		[ReadOnly]
		public ComponentLookup<Creditworthiness> m_Creditworthinesses;

		[ReadOnly]
		public ComponentLookup<PlayerMoney> m_PlayerMoneys;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityEffects;

		public NativeQueue<StatisticsEvent> m_StatisticsEventQueue;

		public NativeQueue<TriggerAction> m_TriggerBuffer;

		public EconomyParameterData m_EconomyParameters;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		public void Execute()
		{
			Loan loan = m_Loans[m_City];
			if (loan.m_Amount > 0)
			{
				float targetInterest = LoanSystem.GetTargetInterest(loan.m_Amount, m_Creditworthinesses[m_City].m_Amount, m_CityEffects[m_City], m_EconomyParameters.m_LoanMinMaxInterestRate);
				Mathf.RoundToInt((float)loan.m_Amount * targetInterest / (float)kUpdatesPerDay);
				PlayerMoney playerMoney = m_PlayerMoneys[m_City];
				if (m_SimulationFrameIndex - loan.m_LastModified > 262144 && playerMoney.money > 0)
				{
					m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.UnpaidLoan, Entity.Null, loan.m_Amount));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Loan> __Game_Simulation_Loan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creditworthiness> __Game_Simulation_Creditworthiness_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_Loan_RO_ComponentLookup = state.GetComponentLookup<Loan>(isReadOnly: true);
			__Game_Simulation_Creditworthiness_RO_ComponentLookup = state.GetComponentLookup<Creditworthiness>(isReadOnly: true);
			__Game_City_PlayerMoney_RO_ComponentLookup = state.GetComponentLookup<PlayerMoney>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private CitySystem m_CitySystem;

	private TriggerSystem m_TriggerSystem;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_EconomyParametersQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EconomyParametersQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_EconomyParametersQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		LoanUpdateJob jobData = new LoanUpdateJob
		{
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps),
			m_City = m_CitySystem.City,
			m_Loans = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Loan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Creditworthinesses = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Creditworthiness_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlayerMoneys = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_PlayerMoney_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer(),
			m_EconomyParameters = m_EconomyParametersQuery.GetSingleton<EconomyParameterData>(),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, deps));
		m_CityStatisticsSystem.AddWriter(base.Dependency);
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
	public LoanUpdateSystem()
	{
	}
}
