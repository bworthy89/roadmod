using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class WealthInfoviewUISystem : InfoviewUISystemBase
{
	private enum Result
	{
		Count,
		Wealth,
		Income,
		Rent,
		Upkeep,
		ResourceCost,
		Fees,
		ResultCount
	}

	private struct UpdateDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Household> m_HouseholdHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourceHandle;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_FeeLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumerLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerLookup;

		public ServiceFeeParameterData m_ServiceFeeParameterData;

		public Entity m_City;

		public NativeArray<long> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Household> nativeArray = chunk.GetNativeArray(ref m_HouseholdHandle);
			NativeArray<PropertyRenter> nativeArray2 = chunk.GetNativeArray(ref m_PropertyRenterHandle);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Household householdData = nativeArray[i];
				PropertyRenter propertyRenter = nativeArray2[i];
				DynamicBuffer<Resources> resources = bufferAccessor[i];
				m_Results[0]++;
				m_Results[1] += EconomyUtils.GetHouseholdTotalWealth(householdData, resources);
				m_Results[2] += householdData.m_SalaryLastDay;
				m_Results[3] += propertyRenter.m_Rent;
				m_Results[4] += householdData.m_MoneySpendOnBuildingLevelingLastDay;
				m_Results[5] += householdData.m_ShoppedValuePerDay;
				int num = 0;
				if (m_FeeLookup.TryGetBuffer(m_City, out var bufferData))
				{
					if (m_WaterConsumerLookup.TryGetComponent(propertyRenter.m_Property, out var componentData))
					{
						num += (int)((float)componentData.m_FulfilledFresh * ServiceFeeSystem.GetFee(PlayerResource.Water, bufferData));
						num += (int)((float)componentData.m_FulfilledSewage * ServiceFeeSystem.GetFee(PlayerResource.Water, bufferData));
					}
					if (m_ElectricityConsumerLookup.TryGetComponent(propertyRenter.m_Property, out var componentData2))
					{
						num += (int)((float)componentData2.m_FulfilledConsumption * ServiceFeeSystem.GetFee(PlayerResource.Electricity, bufferData));
					}
				}
				num += m_ServiceFeeParameterData.m_GarbageFeeRCIO.x;
				m_Results[6] += num;
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
		public ComponentTypeHandle<Household> __Game_Citizens_Household_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Household_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Household>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
		}
	}

	private const string kGroup = "wealthInfo";

	private CitySystem m_CitySystem;

	private EntityQuery m_Query;

	private EntityQuery m_CitizenParametersQuery;

	private GetterValueBinding<string> m_Wealth;

	private GetterValueBinding<int> m_Income;

	private GetterValueBinding<int> m_Rent;

	private GetterValueBinding<int> m_Upkeep;

	private GetterValueBinding<int> m_ResourceCost;

	private GetterValueBinding<int> m_Fees;

	private NativeArray<long> m_Result;

	private CitizenHappinessParameterData m_Parameters;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1755648664_0;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_Wealth.active && !m_Income.active && !m_Rent.active && !m_Upkeep.active && !m_ResourceCost.active)
			{
				return m_Fees.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_Result = new NativeArray<long>(7, Allocator.Persistent);
		m_Query = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.ReadOnly<Resources>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>());
		m_CitizenParametersQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
		RequireForUpdate(m_Query);
		m_Parameters = m_CitizenParametersQuery.GetSingleton<CitizenHappinessParameterData>();
		AddBinding(m_Wealth = new GetterValueBinding<string>("wealthInfo", "averageWealth", GetAverageWealth));
		AddBinding(m_Income = new GetterValueBinding<int>("wealthInfo", "averageIncome", GetAverageIncome));
		AddBinding(m_Rent = new GetterValueBinding<int>("wealthInfo", "averageRent", GetAverageRent));
		AddBinding(m_Upkeep = new GetterValueBinding<int>("wealthInfo", "averageUpkeep", GetAverageUpkeep));
		AddBinding(m_ResourceCost = new GetterValueBinding<int>("wealthInfo", "averageResourceCost", GetAverageResourceCost));
		AddBinding(m_Fees = new GetterValueBinding<int>("wealthInfo", "averageFees", GetAverageFees));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Result.Dispose();
		base.OnDestroy();
	}

	private string GetAverageWealth()
	{
		return CitizenUIUtils.GetHouseholdWealthKey((int)(m_Result[1] / math.max(m_Result[0], 1L)), m_Parameters).ToString();
	}

	private int GetAverageIncome()
	{
		return (int)(m_Result[2] / math.max(m_Result[0], 1L));
	}

	private int GetAverageRent()
	{
		return (int)(m_Result[3] / math.max(m_Result[0], 1L));
	}

	private int GetAverageUpkeep()
	{
		return (int)(m_Result[4] / math.max(m_Result[0], 1L));
	}

	private int GetAverageResourceCost()
	{
		return (int)(m_Result[5] / math.max(m_Result[0], 1L));
	}

	private int GetAverageFees()
	{
		return (int)(m_Result[6] / math.max(m_Result[0], 1L));
	}

	protected override void PerformUpdate()
	{
		ResetResults(m_Result);
		JobChunkExtensions.Schedule(new UpdateDataJob
		{
			m_HouseholdHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_FeeLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
			m_WaterConsumerLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConsumerLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceFeeParameterData = __query_1755648664_0.GetSingleton<ServiceFeeParameterData>(),
			m_City = m_CitySystem.City,
			m_Results = m_Result
		}, m_Query, base.Dependency).Complete();
		m_Wealth.Update();
		m_Income.Update();
		m_Rent.Update();
		m_Upkeep.Update();
		m_ResourceCost.Update();
		m_Fees.Update();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1755648664_0 = entityQueryBuilder2.Build(ref state);
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
	public WealthInfoviewUISystem()
	{
	}
}
