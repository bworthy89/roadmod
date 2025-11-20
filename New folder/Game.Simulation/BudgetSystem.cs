using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Economy;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BudgetSystem : GameSystemBase, IBudgetSystem
{
	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(isReadOnly: true);
		}
	}

	private uint m_LastUpdate;

	private SimulationSystem m_SimulationSystem;

	protected NativeArray<int> m_Trade;

	protected NativeArray<int> m_TradeWorth;

	protected NativeArray<int2> m_HouseholdWealth;

	protected NativeArray<int2> m_ServiceWealth;

	protected NativeArray<int2> m_ProcessingWealth;

	protected int m_TotalTradeWorth;

	protected int m_TotalTaxIncome;

	private NativeArray<int> m_HouseholdCount;

	private NativeArray<int> m_ServiceCount;

	private NativeArray<int> m_ProcessingCount;

	private NativeArray<int2> m_HouseholdWorkers;

	private NativeArray<int2> m_ServiceWorkers;

	private NativeArray<int2> m_ProcessingWorkers;

	private NativeArray<float2> m_CitizenWellbeing;

	private NativeArray<int> m_TouristCount;

	private NativeArray<int> m_TouristIncome;

	private NativeArray<int2> m_LodgingData;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private ResourceSystem m_ResourceSystem;

	private TypeHandle __TypeHandle;

	public bool HasData => m_LastUpdate != 0;

	public uint LastUpdate => m_LastUpdate;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 32768;
	}

	public int GetTotalTradeWorth()
	{
		return m_TotalTradeWorth;
	}

	public int GetHouseholdCount()
	{
		return m_HouseholdCount[0];
	}

	public int GetCompanyCount(bool service, Resource resource)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		if (!service)
		{
			return m_ProcessingCount[resourceIndex];
		}
		return m_ServiceCount[resourceIndex];
	}

	public int2 GetHouseholdWorkers()
	{
		return m_HouseholdWorkers[0];
	}

	public int2 GetCompanyWorkers(bool service, Resource resource)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		if (!service)
		{
			return m_ProcessingWorkers[resourceIndex];
		}
		return m_ServiceWorkers[resourceIndex];
	}

	public float2 GetCitizenWellbeing()
	{
		return m_CitizenWellbeing[0];
	}

	public int GetTrade(Resource resource)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		return m_Trade[resourceIndex];
	}

	public int GetTradeWorth(Resource resource)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		return m_TradeWorth[resourceIndex];
	}

	private void SetTradeWorth(Resource resource, ResourceData resourceData)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		float marketPrice = EconomyUtils.GetMarketPrice(resourceData);
		m_TradeWorth[resourceIndex] = Mathf.RoundToInt(marketPrice * (float)m_Trade[resourceIndex]);
	}

	public int GetHouseholdWealth()
	{
		if (m_HouseholdWealth[0].y > 0)
		{
			return m_HouseholdWealth[0].x / m_HouseholdWealth[0].y;
		}
		return 0;
	}

	public int GetCompanyWealth(bool service, Resource resource)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		int2 @int = (service ? m_ServiceWealth[resourceIndex] : m_ProcessingWealth[resourceIndex]);
		if (@int.y > 0)
		{
			return @int.x / @int.y;
		}
		return 0;
	}

	public int GetTouristCount()
	{
		return m_TouristCount[0];
	}

	public int2 GetLodgingData()
	{
		return m_LodgingData[0];
	}

	public int GetTouristIncome()
	{
		return m_TouristIncome[0];
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		int resourceCount = EconomyUtils.ResourceCount;
		m_Trade = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_TradeWorth = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_HouseholdWealth = new NativeArray<int2>(1, Allocator.Persistent);
		m_ServiceWealth = new NativeArray<int2>(resourceCount, Allocator.Persistent);
		m_ProcessingWealth = new NativeArray<int2>(resourceCount, Allocator.Persistent);
		m_CitizenWellbeing = new NativeArray<float2>(1, Allocator.Persistent);
		m_HouseholdCount = new NativeArray<int>(1, Allocator.Persistent);
		m_HouseholdWorkers = new NativeArray<int2>(1, Allocator.Persistent);
		m_ServiceCount = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ServiceWorkers = new NativeArray<int2>(resourceCount, Allocator.Persistent);
		m_ProcessingCount = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ProcessingWorkers = new NativeArray<int2>(resourceCount, Allocator.Persistent);
		m_TouristCount = new NativeArray<int>(1, Allocator.Persistent);
		m_TouristIncome = new NativeArray<int>(1, Allocator.Persistent);
		m_LodgingData = new NativeArray<int2>(1, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Trade.Dispose();
		m_TradeWorth.Dispose();
		m_HouseholdWealth.Dispose();
		m_ServiceWealth.Dispose();
		m_ProcessingWealth.Dispose();
		m_CitizenWellbeing.Dispose();
		m_HouseholdCount.Dispose();
		m_HouseholdWorkers.Dispose();
		m_ServiceCount.Dispose();
		m_ServiceWorkers.Dispose();
		m_ProcessingCount.Dispose();
		m_ProcessingWorkers.Dispose();
		m_TouristCount.Dispose();
		m_TouristIncome.Dispose();
		m_LodgingData.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateData();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		UpdateData();
	}

	private void UpdateData()
	{
		m_LastUpdate = m_SimulationSystem.frameIndex;
		InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef);
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		ResourceIterator iterator = ResourceIterator.GetIterator();
		while (iterator.Next())
		{
			if (base.EntityManager.HasComponent<ResourceData>(prefabs[iterator.resource]) && base.EntityManager.GetComponentData<ResourceData>(prefabs[iterator.resource]).m_IsTradable)
			{
				m_Trade[EconomyUtils.GetResourceIndex(iterator.resource)] = m_CityStatisticsSystem.GetStatisticValue(StatisticType.Trade, EconomyUtils.GetResourceIndex(iterator.resource));
			}
		}
		iterator = ResourceIterator.GetIterator();
		while (iterator.Next())
		{
			if (base.EntityManager.HasComponent<ResourceData>(prefabs[iterator.resource]))
			{
				ResourceData componentData = base.EntityManager.GetComponentData<ResourceData>(prefabs[iterator.resource]);
				if (componentData.m_IsTradable)
				{
					SetTradeWorth(iterator.resource, componentData);
				}
			}
		}
		int statisticValue = m_CityStatisticsSystem.GetStatisticValue(StatisticType.HouseholdCount);
		m_HouseholdWealth[0] = new int2(m_CityStatisticsSystem.GetStatisticValue(StatisticType.HouseholdWealth), statisticValue);
		m_HouseholdCount[0] = statisticValue;
		int statisticValue2 = m_CityStatisticsSystem.GetStatisticValue(StatisticType.Population);
		if (statisticValue2 > 0)
		{
			m_CitizenWellbeing[0] = new float2((float)m_CityStatisticsSystem.GetStatisticValue(StatisticType.WellbeingLevel) / (float)statisticValue2, (float)m_CityStatisticsSystem.GetStatisticValue(StatisticType.HealthLevel) / (float)statisticValue2);
		}
		statisticValue = m_CityStatisticsSystem.GetStatisticValue(StatisticType.WorkerCount);
		m_HouseholdWorkers[0] = new int2(statisticValue, statisticValue + m_CityStatisticsSystem.GetStatisticValue(StatisticType.Unemployed));
		iterator = ResourceIterator.GetIterator();
		while (iterator.Next())
		{
			int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
			m_ServiceWealth[resourceIndex] = new int2(m_CityStatisticsSystem.GetStatisticValue(StatisticType.ServiceWealth, resourceIndex), m_CityStatisticsSystem.GetStatisticValue(StatisticType.ServiceCount, resourceIndex));
			m_ProcessingWealth[resourceIndex] = new int2(m_CityStatisticsSystem.GetStatisticValue(StatisticType.ProcessingWealth, resourceIndex), m_CityStatisticsSystem.GetStatisticValue(StatisticType.ProcessingCount, resourceIndex));
			m_ServiceCount[resourceIndex] = m_CityStatisticsSystem.GetStatisticValue(StatisticType.ServiceCount, resourceIndex);
			m_ServiceWorkers[resourceIndex] = new int2(m_CityStatisticsSystem.GetStatisticValue(StatisticType.ServiceWorkers, resourceIndex), m_CityStatisticsSystem.GetStatisticValue(StatisticType.ServiceMaxWorkers, resourceIndex));
			m_ProcessingCount[resourceIndex] = m_CityStatisticsSystem.GetStatisticValue(StatisticType.ProcessingCount, resourceIndex);
			m_ProcessingWorkers[resourceIndex] = new int2(m_CityStatisticsSystem.GetStatisticValue(StatisticType.ProcessingWorkers, resourceIndex), m_CityStatisticsSystem.GetStatisticValue(StatisticType.ProcessingMaxWorkers, resourceIndex));
		}
		m_TouristIncome[0] = m_CityStatisticsSystem.GetStatisticValue(StatisticType.TouristIncome);
		m_LodgingData[0] = new int2(m_CityStatisticsSystem.GetStatisticValue(StatisticType.LodgingUsed), m_CityStatisticsSystem.GetStatisticValue(StatisticType.LodgingTotal));
		UpdateTotalTradeWorth();
	}

	private void UpdateTotalTradeWorth()
	{
		m_TotalTradeWorth = 0;
		for (int i = 0; i < m_TradeWorth.Length; i++)
		{
			m_TotalTradeWorth += m_TradeWorth[i];
		}
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
	public BudgetSystem()
	{
	}
}
