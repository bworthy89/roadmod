using System;
using System.Collections.Generic;
using Game.Areas;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { typeof(AreaPrefab) })]
public class MasterArea : ComponentBase
{
	public SlaveAreaInfo[] m_SlaveAreas;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_SlaveAreas.Length; i++)
		{
			prefabs.Add(m_SlaveAreas[i].m_Area);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SubArea>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Areas.SubArea>());
	}
}
