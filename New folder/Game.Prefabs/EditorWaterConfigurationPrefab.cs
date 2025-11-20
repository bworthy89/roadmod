using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class EditorWaterConfigurationPrefab : PrefabBase
{
	public InfoviewPrefab m_WaterInfoview;

	public InfomodeInfo m_WaterFlowInfo;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_WaterInfoview);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<EditorWaterConfigurationData>());
	}
}
