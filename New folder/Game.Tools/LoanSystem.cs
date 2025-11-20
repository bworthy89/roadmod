using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class LoanSystem : GameSystemBase, ILoanSystem
{
	private struct LoanActionJob : IJob
	{
		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		public NativeQueue<LoanAction> m_ActionQueue;

		public ComponentLookup<Loan> m_Loans;

		public ComponentLookup<PlayerMoney> m_Money;

		public void Execute()
		{
			LoanAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				PlayerMoney value = m_Money[m_City];
				value.Add(item.m_Amount - m_Loans[m_City].m_Amount);
				m_Money[m_City] = value;
				m_Loans[m_City] = new Loan
				{
					m_Amount = item.m_Amount,
					m_LastModified = m_SimulationFrameIndex
				};
			}
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<Loan> __Game_Simulation_Loan_RW_ComponentLookup;

		public ComponentLookup<PlayerMoney> __Game_City_PlayerMoney_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_Loan_RW_ComponentLookup = state.GetComponentLookup<Loan>();
			__Game_City_PlayerMoney_RW_ComponentLookup = state.GetComponentLookup<PlayerMoney>();
		}
	}

	private CitySystem m_CitySystem;

	private SimulationSystem m_SimulationSystem;

	private NativeQueue<LoanAction> m_ActionQueue;

	private JobHandle m_ActionQueueWriters;

	private EntityQuery m_EconomyParametersQuery;

	private TypeHandle __TypeHandle;

	public LoanInfo CurrentLoan
	{
		get
		{
			if (base.EntityManager.TryGetComponent<Loan>(m_CitySystem.City, out var component))
			{
				return CalculateLoan(component.m_Amount);
			}
			return default(LoanInfo);
		}
	}

	public int Creditworthiness => base.EntityManager.GetComponentData<Creditworthiness>(m_CitySystem.City).m_Amount;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EconomyParametersQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ActionQueue = new NativeQueue<LoanAction>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_ActionQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ActionQueue.IsEmpty())
		{
			LoanActionJob jobData = new LoanActionJob
			{
				m_City = m_CitySystem.City,
				m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
				m_ActionQueue = m_ActionQueue,
				m_Loans = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Loan_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Money = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_PlayerMoney_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(m_ActionQueueWriters, base.Dependency));
			m_ActionQueueWriters = base.Dependency;
		}
	}

	public LoanInfo RequestLoanOffer(int amount)
	{
		return CalculateLoan(ClampLoanAmount(amount));
	}

	public void ChangeLoan(int amount)
	{
		m_ActionQueueWriters.Complete();
		m_ActionQueue.Enqueue(new LoanAction
		{
			m_Amount = ClampLoanAmount(amount)
		});
	}

	private int ClampLoanAmount(int amount)
	{
		PlayerMoney componentData = base.EntityManager.GetComponentData<PlayerMoney>(m_CitySystem.City);
		int lowerBound = math.max(0, CurrentLoan.m_Amount - math.max(0, componentData.money));
		return math.clamp(amount, lowerBound, Creditworthiness);
	}

	public LoanInfo CalculateLoan(int amount)
	{
		float2 loanMinMaxInterestRate = m_EconomyParametersQuery.GetSingleton<EconomyParameterData>().m_LoanMinMaxInterestRate;
		return CalculateLoan(amount, Creditworthiness, base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true), loanMinMaxInterestRate);
	}

	public static LoanInfo CalculateLoan(int amount, int creditworthiness, DynamicBuffer<CityModifier> modifiers, float2 interestRange)
	{
		if (amount > 0)
		{
			float targetInterest = GetTargetInterest(amount, creditworthiness, modifiers, interestRange);
			return new LoanInfo
			{
				m_Amount = amount,
				m_DailyInterestRate = targetInterest,
				m_DailyPayment = Mathf.RoundToInt((float)amount * targetInterest)
			};
		}
		return default(LoanInfo);
	}

	public static float GetTargetInterest(int loanAmount, int creditworthiness, DynamicBuffer<CityModifier> cityEffects, float2 interestRange)
	{
		float value = 100f * math.lerp(interestRange.x, interestRange.y, math.saturate((float)loanAmount / math.max(1f, creditworthiness)));
		CityUtils.ApplyModifier(ref value, cityEffects, CityModifierType.LoanInterest);
		return math.max(0f, 0.01f * value);
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
	public LoanSystem()
	{
	}
}
