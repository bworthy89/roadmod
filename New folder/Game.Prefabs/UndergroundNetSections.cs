using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetGeometryPrefab) })]
public class UndergroundNetSections : ComponentBase
{
	public NetSectionInfo[] m_Sections;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Sections.Length; i++)
		{
			prefabs.Add(m_Sections[i].m_Section);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
