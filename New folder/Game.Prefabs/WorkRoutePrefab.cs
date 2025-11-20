using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Net;
using Game.Pathfind;
using Game.Policies;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { })]
public class WorkRoutePrefab : RoutePrefab
{
	public RoadTypes m_RouteRoadType = RoadTypes.Car;

	public SizeClass m_SizeClass = SizeClass.Large;

	public MapFeature m_MapFeature = MapFeature.None;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<RouteConnectionData>());
		components.Add(ComponentType.ReadWrite<WorkRouteData>());
		components.Add(ComponentType.ReadWrite<PlaceableInfoviewItem>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		if (components.Contains(ComponentType.ReadWrite<Route>()))
		{
			components.Add(ComponentType.ReadWrite<WorkRoute>());
			components.Add(ComponentType.ReadWrite<RouteVehicle>());
			components.Add(ComponentType.ReadWrite<RouteNumber>());
			components.Add(ComponentType.ReadWrite<VehicleModel>());
			components.Add(ComponentType.ReadWrite<RouteModifier>());
			components.Add(ComponentType.ReadWrite<Policy>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Waypoint>()))
		{
			components.Add(ComponentType.ReadWrite<AccessLane>());
			components.Add(ComponentType.ReadWrite<RouteLane>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Game.Routes.Segment>()))
		{
			components.Add(ComponentType.ReadWrite<PathTargets>());
			components.Add(ComponentType.ReadWrite<RouteInfo>());
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
		entityManager.SetComponentData(entity, new WorkRouteData
		{
			m_RoadType = m_RouteRoadType,
			m_SizeClass = m_SizeClass,
			m_MapFeature = m_MapFeature
		});
	}
}
