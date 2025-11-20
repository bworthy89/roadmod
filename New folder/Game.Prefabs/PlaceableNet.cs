using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPrefab) })]
public class PlaceableNet : ComponentBase
{
	public Bounds1 m_ElevationRange = new Bounds1(-50f, 50f);

	public bool m_AllowParallelMode = true;

	public NetPrefab m_UndergroundPrefab;

	public int m_XPReward;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_UndergroundPrefab != null)
		{
			prefabs.Add(m_UndergroundPrefab);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceableNetData>());
		components.Add(ComponentType.ReadWrite<PlaceableInfoviewItem>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (components.Contains(ComponentType.ReadWrite<NetCompositionData>()))
		{
			components.Add(ComponentType.ReadWrite<PlaceableNetComposition>());
		}
	}
}
