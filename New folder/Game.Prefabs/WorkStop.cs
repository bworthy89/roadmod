using System;
using System.Collections.Generic;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { typeof(ObjectPrefab) })]
public class WorkStop : ComponentBase
{
	public RoadTypes m_RouteRoadType = RoadTypes.Car;

	public bool m_WorkLocation;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WorkStopData>());
		components.Add(ComponentType.ReadWrite<TransportStopData>());
		components.Add(ComponentType.ReadWrite<RouteConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.Color>());
		components.Add(ComponentType.ReadWrite<Game.Routes.TransportStop>());
		components.Add(ComponentType.ReadWrite<Game.Routes.WorkStop>());
		components.Add(ComponentType.ReadWrite<ConnectedRoute>());
		components.Add(ComponentType.ReadWrite<BoardingVehicle>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		RouteConnectionData componentData = default(RouteConnectionData);
		componentData.m_AccessConnectionType = RouteConnectionType.Pedestrian;
		componentData.m_RouteConnectionType = RouteConnectionType.Road;
		componentData.m_AccessTrackType = TrackTypes.None;
		componentData.m_RouteTrackType = TrackTypes.None;
		componentData.m_AccessRoadType = RoadTypes.None;
		componentData.m_RouteRoadType = m_RouteRoadType;
		componentData.m_RouteSizeClass = SizeClass.Undefined;
		componentData.m_StartLaneOffset = 0f;
		componentData.m_EndMargin = 0f;
		TransportStopData componentData2 = default(TransportStopData);
		componentData2.m_ComfortFactor = 0f;
		componentData2.m_LoadingFactor = 0f;
		componentData2.m_AccessDistance = 0f;
		componentData2.m_BoardingTime = 0f;
		componentData2.m_TransportType = TransportType.Work;
		componentData2.m_PassengerTransport = false;
		componentData2.m_CargoTransport = true;
		WorkStopData componentData3 = default(WorkStopData);
		componentData3.m_WorkLocation = m_WorkLocation;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, componentData2);
		entityManager.SetComponentData(entity, componentData3);
	}
}
