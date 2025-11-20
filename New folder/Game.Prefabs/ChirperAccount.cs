using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

public class ChirperAccount : PrefabBase
{
	public InfoviewPrefab m_InfoView;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ChirperAccountData>());
	}
}
