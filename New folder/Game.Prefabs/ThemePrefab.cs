using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Themes/", new Type[] { })]
public class ThemePrefab : PrefabBase
{
	public string assetPrefix;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ThemeData>());
	}
}
