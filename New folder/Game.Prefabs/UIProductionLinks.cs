using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { typeof(ResourcePrefab) })]
public class UIProductionLinks : ComponentBase
{
	public UIProductionLinkPrefab m_Producer;

	[CanBeNull]
	public UIProductionLinkPrefab[] m_FinalConsumers;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Producer != null)
		{
			prefabs.Add(m_Producer);
		}
		if (m_FinalConsumers == null)
		{
			return;
		}
		for (int i = 0; i < m_FinalConsumers.Length; i++)
		{
			UIProductionLinkPrefab uIProductionLinkPrefab = m_FinalConsumers[i];
			if (uIProductionLinkPrefab != null)
			{
				prefabs.Add(uIProductionLinkPrefab);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<UIProductionLinksData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
