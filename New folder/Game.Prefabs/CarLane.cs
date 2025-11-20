using System;
using System.Collections.Generic;
using Game.Net;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetLanePrefab) })]
public class CarLane : ComponentBase
{
	public NetLanePrefab m_NotTrackLane;

	public NetLanePrefab m_NotBusLane;

	public RoadTypes m_RoadType = RoadTypes.Car;

	public SizeClass m_MaxSize = SizeClass.Large;

	public float m_Width = 3f;

	public bool m_StartingLane;

	public bool m_EndingLane;

	public bool m_Twoway;

	public bool m_BusLane;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_NotTrackLane != null)
		{
			prefabs.Add(m_NotTrackLane);
		}
		if (m_NotBusLane != null)
		{
			prefabs.Add(m_NotBusLane);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CarLaneData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Net.CarLane>());
		if (components.Contains(ComponentType.ReadWrite<MasterLane>()))
		{
			return;
		}
		components.Add(ComponentType.ReadWrite<LaneObject>());
		components.Add(ComponentType.ReadWrite<LaneReservation>());
		components.Add(ComponentType.ReadWrite<LaneOverlap>());
		components.Add(ComponentType.ReadWrite<UpdateFrame>());
		if (m_RoadType == RoadTypes.Bicycle && base.prefab is NetLaneGeometryPrefab)
		{
			components.Add(ComponentType.ReadWrite<LaneColor>());
		}
		if (base.prefab is NetLanePrefab netLanePrefab && netLanePrefab.m_PathfindPrefab != null && netLanePrefab.m_PathfindPrefab.m_TrackTrafficFlow)
		{
			if ((m_RoadType & ~RoadTypes.Bicycle) != RoadTypes.None)
			{
				components.Add(ComponentType.ReadWrite<LaneFlow>());
			}
			if ((m_RoadType & RoadTypes.Bicycle) != RoadTypes.None)
			{
				components.Add(ComponentType.ReadWrite<SecondaryFlow>());
			}
		}
	}
}
