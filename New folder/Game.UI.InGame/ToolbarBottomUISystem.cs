using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.UI.Binding;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ToolbarBottomUISystem : UISystemBase
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TypeHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
		}
	}

	private const string kGroup = "toolbarBottom";

	private PrefabSystem m_PrefabSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ICityStatisticsSystem m_CityStatisticsSystem;

	private CitySystem m_CitySystem;

	private ICityServiceBudgetSystem m_CityServiceBudgetSystem;

	private GetterValueBinding<string> m_CityNameBinding;

	private GetterValueBinding<int> m_MoneyBinding;

	private GetterValueBinding<int> m_MoneyDeltaBinding;

	private GetterValueBinding<int> m_PopulationBinding;

	private GetterValueBinding<int> m_PopulationDeltaBinding;

	private GetterValueBinding<bool> m_UnlimitedMoneyBinding;

	private UIToolbarBottomConfigurationPrefab m_ToolbarBottomConfigurationPrefab;

	private EntityQuery m_ToolbarBottomConfigurationQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_2118611066_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityServiceBudgetSystem = base.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
		m_ToolbarBottomConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<UIToolbarBottomConfigurationData>());
		AddBinding(m_CityNameBinding = new GetterValueBinding<string>("toolbarBottom", "cityName", () => m_CityConfigurationSystem.cityName ?? ""));
		AddBinding(m_MoneyBinding = new GetterValueBinding<int>("toolbarBottom", "money", () => m_CitySystem.moneyAmount));
		AddBinding(m_MoneyDeltaBinding = new GetterValueBinding<int>("toolbarBottom", "moneyDelta", m_CityServiceBudgetSystem.GetMoneyDelta));
		AddBinding(m_UnlimitedMoneyBinding = new GetterValueBinding<bool>("toolbarBottom", "unlimitedMoney", () => m_CityConfigurationSystem.unlimitedMoney));
		AddBinding(m_PopulationBinding = new GetterValueBinding<int>("toolbarBottom", "population", GetPopulation));
		AddBinding(m_PopulationDeltaBinding = new GetterValueBinding<int>("toolbarBottom", "populationDelta", GetPopulationDelta));
		AddBinding(new GetterValueBinding<float2>("toolbarBottom", "populationTrendThresholds", () => new float2(m_ToolbarBottomConfigurationPrefab.m_PopulationTrendThresholds.m_Medium, m_ToolbarBottomConfigurationPrefab.m_PopulationTrendThresholds.m_High)));
		AddBinding(new GetterValueBinding<float2>("toolbarBottom", "moneyTrendThresholds", () => new float2(m_ToolbarBottomConfigurationPrefab.m_MoneyTrendThresholds.m_Medium, m_ToolbarBottomConfigurationPrefab.m_MoneyTrendThresholds.m_High)));
		AddBinding(new TriggerBinding<string>("toolbarBottom", "setCityName", SetCityName));
		RequireForUpdate(m_ToolbarBottomConfigurationQuery);
	}

	private int GetPopulation()
	{
		if (base.EntityManager.HasComponent<Population>(m_CitySystem.City))
		{
			return base.EntityManager.GetComponentData<Population>(m_CitySystem.City).m_Population;
		}
		return 0;
	}

	private int GetPopulationDelta()
	{
		Population population = default(Population);
		if (base.EntityManager.HasComponent<Population>(m_CitySystem.City))
		{
			population = base.EntityManager.GetComponentData<Population>(m_CitySystem.City);
		}
		NativeArray<int> statisticDataArray = m_CityStatisticsSystem.GetStatisticDataArray(StatisticType.Population);
		if (statisticDataArray.Length == 0)
		{
			return population.m_Population;
		}
		int num = ((statisticDataArray.Length >= 2) ? statisticDataArray[statisticDataArray.Length - 2] : 0);
		int num2 = statisticDataArray[statisticDataArray.Length - 1];
		float t = (float)(long)(m_CityStatisticsSystem.GetSampleFrameIndex(m_CityStatisticsSystem.sampleCount - 1) % 8192) / 8192f;
		return (population.m_Population - Mathf.RoundToInt(math.lerp(num, num2, t))) * 32 / 24;
	}

	private void SetCityName(string name)
	{
		m_CityConfigurationSystem.cityName = name;
		m_CityNameBinding.Update();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_CityNameBinding.Update();
		m_MoneyBinding.Update();
		m_PopulationBinding.Update();
		m_MoneyDeltaBinding.Update();
		m_PopulationDeltaBinding.Update();
		m_UnlimitedMoneyBinding.Update();
		if (m_ToolbarBottomConfigurationPrefab == null)
		{
			Entity singletonEntity = __query_2118611066_0.GetSingletonEntity();
			m_ToolbarBottomConfigurationPrefab = m_PrefabSystem.GetPrefab<UIToolbarBottomConfigurationPrefab>(singletonEntity);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<UIToolbarBottomConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2118611066_0 = entityQueryBuilder2.Build(ref state);
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
	public ToolbarBottomUISystem()
	{
	}
}
