using System;
using System.Collections.Generic;
using Game.Net;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { typeof(MarkerObjectPrefab) })]
public class TakeoffLocation : ComponentBase
{
	public RouteConnectionType m_ConnectionType1 = RouteConnectionType.Road;

	public RouteConnectionType m_ConnectionType2 = RouteConnectionType.Air;

	public RoadTypes m_RoadType;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<RouteConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Routes.TakeoffLocation>());
		components.Add(ComponentType.ReadWrite<AccessLane>());
		components.Add(ComponentType.ReadWrite<RouteLane>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		RouteConnectionData componentData = default(RouteConnectionData);
		componentData.m_AccessConnectionType = m_ConnectionType1;
		componentData.m_RouteConnectionType = m_ConnectionType2;
		componentData.m_AccessTrackType = TrackTypes.None;
		componentData.m_RouteTrackType = TrackTypes.None;
		componentData.m_AccessRoadType = m_RoadType;
		componentData.m_RouteRoadType = m_RoadType;
		componentData.m_RouteSizeClass = SizeClass.Undefined;
		componentData.m_StartLaneOffset = 0f;
		componentData.m_EndMargin = 0f;
		entityManager.SetComponentData(entity, componentData);
	}
}
