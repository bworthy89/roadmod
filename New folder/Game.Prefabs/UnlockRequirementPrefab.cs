using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

public abstract class UnlockRequirementPrefab : PrefabBase
{
	public string m_LabelID;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<UnlockRequirementData>());
	}
}
