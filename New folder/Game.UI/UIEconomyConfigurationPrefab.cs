using System;
using System.Collections.Generic;
using Game.City;
using Game.Prefabs;
using Unity.Entities;

namespace Game.UI;

[ComponentMenu("Settings/", new Type[] { })]
public class UIEconomyConfigurationPrefab : PrefabBase
{
	public BudgetItem<IncomeSource>[] m_IncomeItems;

	public BudgetItem<ExpenseSource>[] m_ExpenseItems;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<UIEconomyConfigurationData>());
	}
}
