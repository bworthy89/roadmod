using System;
using System.Collections.Generic;
using Game.Areas;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class ObjectSubAreas : ComponentBase
{
	public ObjectSubAreaInfo[] m_SubAreas;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_SubAreas != null)
		{
			for (int i = 0; i < m_SubAreas.Length; i++)
			{
				prefabs.Add(m_SubAreas[i].m_AreaPrefab);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SubArea>());
		components.Add(ComponentType.ReadWrite<SubAreaNode>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Areas.SubArea>());
	}
}
