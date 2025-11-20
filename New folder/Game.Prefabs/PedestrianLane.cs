using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetLanePrefab) })]
public class PedestrianLane : ComponentBase
{
	public NetLanePrefab m_NotWalkLane;

	public float m_Width;

	public bool m_OnWater;

	public ActivityType[] m_Activities;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_NotWalkLane != null)
		{
			prefabs.Add(m_NotWalkLane);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PedestrianLaneData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Net.PedestrianLane>());
		components.Add(ComponentType.ReadWrite<LaneObject>());
		components.Add(ComponentType.ReadWrite<LaneOverlap>());
	}
}
