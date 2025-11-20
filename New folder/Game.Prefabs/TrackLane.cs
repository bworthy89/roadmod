using System;
using System.Collections.Generic;
using Game.Net;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetLanePrefab) })]
public class TrackLane : ComponentBase
{
	public NetLanePrefab m_FallbackLane;

	public ObjectPrefab m_EndObject;

	public TrackTypes m_TrackType = TrackTypes.Train;

	public float m_Width = 4f;

	public float m_MaxCurviness = 1.8f;

	public bool m_Twoway;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_FallbackLane != null)
		{
			prefabs.Add(m_FallbackLane);
		}
		if (m_EndObject != null)
		{
			prefabs.Add(m_EndObject);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TrackLaneData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Net.TrackLane>());
		if (!components.Contains(ComponentType.ReadWrite<MasterLane>()))
		{
			components.Add(ComponentType.ReadWrite<LaneObject>());
			components.Add(ComponentType.ReadWrite<LaneReservation>());
			components.Add(ComponentType.ReadWrite<LaneColor>());
			components.Add(ComponentType.ReadWrite<LaneOverlap>());
			components.Add(ComponentType.ReadWrite<UpdateFrame>());
		}
	}
}
