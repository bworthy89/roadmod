using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class BudgetUISystem : UISystemBase
{
	private const string kGroup = "budget";

	private PrefabSystem m_PrefabSystem;

	private GameModeGovernmentSubsidiesSystem m_GovernmentSubsidiesSystem;

	private ICityServiceBudgetSystem m_CityServiceBudgetSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private MapTilePurchaseSystem m_MapTilePurchaseSystem;

	private EntityQuery m_ConfigQuery;

	private GetterValueBinding<int> m_TotalIncomeBinding;

	private GetterValueBinding<int> m_TotalExpensesBinding;

	private RawValueBinding m_IncomeItemsBinding;

	private RawValueBinding m_IncomeValuesBinding;

	private RawValueBinding m_ExpenseItemsBinding;

	private RawValueBinding m_ExpenseValuesBinding;

	private Dictionary<string, Func<bool>> m_BudgetsActivations;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_CityServiceBudgetSystem = base.World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
		m_GovernmentSubsidiesSystem = base.World.GetOrCreateSystemManaged<GameModeGovernmentSubsidiesSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_MapTilePurchaseSystem = base.World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIEconomyConfigurationData>());
		m_BudgetsActivations = new Dictionary<string, Func<bool>>
		{
			{ "Government", m_GovernmentSubsidiesSystem.GetGovernmentSubsidiesEnabled },
			{
				"Loan Interest",
				() => !m_CityConfigurationSystem.unlimitedMoney
			},
			{ "Tile Upkeep", m_MapTilePurchaseSystem.GetMapTileUpkeepEnabled }
		};
		AddBinding(m_TotalIncomeBinding = new GetterValueBinding<int>("budget", "totalIncome", () => m_CityServiceBudgetSystem.GetTotalIncome()));
		AddBinding(m_TotalExpensesBinding = new GetterValueBinding<int>("budget", "totalExpenses", () => m_CityServiceBudgetSystem.GetTotalExpenses()));
		AddBinding(m_IncomeItemsBinding = new RawValueBinding("budget", "incomeItems", BindIncomeItems));
		AddBinding(m_IncomeValuesBinding = new RawValueBinding("budget", "incomeValues", BindIncomeValues));
		AddBinding(m_ExpenseItemsBinding = new RawValueBinding("budget", "expenseItems", BindExpenseItems));
		AddBinding(m_ExpenseValuesBinding = new RawValueBinding("budget", "expenseValues", BindExpenseValues));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_TotalIncomeBinding.Update();
		m_TotalExpensesBinding.Update();
		m_IncomeValuesBinding.Update();
		m_ExpenseValuesBinding.Update();
	}

	private void BindIncomeItems(IJsonWriter writer)
	{
		UIEconomyConfigurationPrefab config = GetConfig();
		writer.ArrayBegin(config.m_IncomeItems.Length);
		BudgetItem<IncomeSource>[] incomeItems = config.m_IncomeItems;
		foreach (BudgetItem<IncomeSource> budgetItem in incomeItems)
		{
			writer.TypeBegin("Game.UI.InGame.BudgetItem");
			writer.PropertyName("id");
			writer.Write(budgetItem.m_ID);
			writer.PropertyName("color");
			writer.Write(budgetItem.m_Color);
			writer.PropertyName("icon");
			writer.Write(budgetItem.m_Icon);
			writer.PropertyName("active");
			writer.Write(!m_BudgetsActivations.ContainsKey(budgetItem.m_ID) || m_BudgetsActivations[budgetItem.m_ID]());
			writer.PropertyName("sources");
			writer.ArrayBegin(budgetItem.m_Sources.Length);
			IncomeSource[] sources = budgetItem.m_Sources;
			foreach (IncomeSource incomeSource in sources)
			{
				writer.TypeBegin("Game.UI.InGame.BudgetSource");
				writer.PropertyName("id");
				writer.Write(Enum.GetName(typeof(IncomeSource), incomeSource));
				writer.PropertyName("index");
				writer.Write((int)incomeSource);
				writer.TypeEnd();
			}
			writer.ArrayEnd();
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	private void BindIncomeValues(IJsonWriter writer)
	{
		writer.ArrayBegin(14u);
		for (int i = 0; i < 14; i++)
		{
			writer.Write(m_CityServiceBudgetSystem.GetIncome((IncomeSource)i));
		}
		writer.ArrayEnd();
	}

	private void BindExpenseItems(IJsonWriter writer)
	{
		UIEconomyConfigurationPrefab config = GetConfig();
		writer.ArrayBegin(config.m_ExpenseItems.Length);
		BudgetItem<ExpenseSource>[] expenseItems = config.m_ExpenseItems;
		foreach (BudgetItem<ExpenseSource> budgetItem in expenseItems)
		{
			writer.TypeBegin("Game.UI.InGame.BudgetItem");
			writer.PropertyName("id");
			writer.Write(budgetItem.m_ID);
			writer.PropertyName("color");
			writer.Write(budgetItem.m_Color);
			writer.PropertyName("icon");
			writer.Write(budgetItem.m_Icon);
			writer.PropertyName("active");
			writer.Write(!m_BudgetsActivations.ContainsKey(budgetItem.m_ID) || m_BudgetsActivations[budgetItem.m_ID]());
			writer.PropertyName("sources");
			writer.ArrayBegin(budgetItem.m_Sources.Length);
			ExpenseSource[] sources = budgetItem.m_Sources;
			foreach (ExpenseSource expenseSource in sources)
			{
				writer.TypeBegin("Game.UI.InGame.BudgetSource");
				writer.PropertyName("id");
				writer.Write(Enum.GetName(typeof(ExpenseSource), expenseSource));
				writer.PropertyName("index");
				writer.Write((int)expenseSource);
				writer.TypeEnd();
			}
			writer.ArrayEnd();
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	private void BindExpenseValues(IJsonWriter writer)
	{
		writer.ArrayBegin(15u);
		for (int i = 0; i < 15; i++)
		{
			writer.Write(-m_CityServiceBudgetSystem.GetExpense((ExpenseSource)i));
		}
		writer.ArrayEnd();
	}

	private UIEconomyConfigurationPrefab GetConfig()
	{
		return m_PrefabSystem.GetSingletonPrefab<UIEconomyConfigurationPrefab>(m_ConfigQuery);
	}

	[Preserve]
	public BudgetUISystem()
	{
	}
}
