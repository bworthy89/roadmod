using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetLanePrefab) })]
public class SpawnableLane : ComponentBase
{
	public NetLanePrefab[] m_Placeholders;

	[Range(0f, 100f)]
	public int m_Probability = 100;

	public GroupPrefab m_RandomizationGroup;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Placeholders.Length; i++)
		{
			prefabs.Add(m_Placeholders[i]);
		}
		if (m_RandomizationGroup != null)
		{
			prefabs.Add(m_RandomizationGroup);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SpawnableObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
