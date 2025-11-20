using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class UIToolbarBottomConfigurationPrefab : PrefabBase
{
	public UITrendThresholds m_MoneyTrendThresholds;

	public UITrendThresholds m_PopulationTrendThresholds;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<UIToolbarBottomConfigurationData>());
	}
}
