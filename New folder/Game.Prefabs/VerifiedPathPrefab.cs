using System;
using System.Collections.Generic;
using Game.Net;
using Game.Pathfind;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { })]
public class VerifiedPathPrefab : RoutePrefab
{
	public RoadTypes m_RouteRoadType = RoadTypes.Car;

	public SizeClass m_SizeClass = SizeClass.Large;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<RouteConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		if (components.Contains(ComponentType.ReadWrite<Route>()))
		{
			components.Add(ComponentType.ReadWrite<VerifiedPath>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Waypoint>()))
		{
			components.Add(ComponentType.ReadWrite<VerifiedPath>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Game.Routes.Segment>()))
		{
			components.Add(ComponentType.ReadWrite<VerifiedPath>());
			components.Add(ComponentType.ReadWrite<PathElement>());
			components.Add(ComponentType.ReadWrite<PathInformation>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new RouteConnectionData
		{
			m_AccessConnectionType = RouteConnectionType.Pedestrian,
			m_RouteConnectionType = RouteConnectionType.Road,
			m_AccessTrackType = TrackTypes.None,
			m_RouteTrackType = TrackTypes.None,
			m_AccessRoadType = RoadTypes.None,
			m_RouteRoadType = m_RouteRoadType,
			m_RouteSizeClass = m_SizeClass,
			m_StartLaneOffset = 0f,
			m_EndMargin = 0f
		});
	}
}
