using System;
using System.Collections.Generic;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { typeof(ObjectPrefab) })]
public class TransportStop : ComponentBase
{
	public TransportType m_TransportType;

	public RouteConnectionType m_AccessConnectionType = RouteConnectionType.Pedestrian;

	public RouteConnectionType m_RouteConnectionType = RouteConnectionType.Road;

	public TrackTypes m_AccessTrackType;

	public TrackTypes m_RouteTrackType;

	public RoadTypes m_AccessRoadType;

	public RoadTypes m_RouteRoadType;

	public float m_EnterDistance;

	public float m_ExitDistance;

	public float m_AccessDistance;

	public float m_BoardingTime;

	public float m_ComfortFactor;

	public float m_LoadingFactor;

	public bool m_PassengerTransport = true;

	public bool m_CargoTransport;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TransportStopData>());
		components.Add(ComponentType.ReadWrite<RouteConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Routes.TransportStop>());
		components.Add(ComponentType.ReadWrite<Game.Objects.Color>());
		switch (m_TransportType)
		{
		case TransportType.Bus:
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<BusStop>());
			break;
		case TransportType.Train:
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<TrainStop>());
			break;
		case TransportType.Taxi:
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<RouteVehicle>());
			components.Add(ComponentType.ReadWrite<TaxiStand>());
			components.Add(ComponentType.ReadWrite<DispatchedRequest>());
			if (m_AccessConnectionType != RouteConnectionType.None)
			{
				components.Add(ComponentType.ReadWrite<AccessLane>());
			}
			if (m_RouteConnectionType != RouteConnectionType.None)
			{
				components.Add(ComponentType.ReadWrite<RouteLane>());
			}
			if (m_PassengerTransport)
			{
				components.Add(ComponentType.ReadWrite<WaitingPassengers>());
			}
			break;
		case TransportType.Tram:
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<TramStop>());
			break;
		case TransportType.Ship:
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<ShipStop>());
			break;
		case TransportType.Helicopter:
		case TransportType.Rocket:
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			break;
		case TransportType.Airplane:
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			components.Add(ComponentType.ReadWrite<AirplaneStop>());
			if (GetComponent<OutsideConnection>() != null)
			{
				components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
			}
			break;
		case TransportType.Subway:
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<SubwayStop>());
			break;
		case TransportType.Ferry:
			components.Add(ComponentType.ReadWrite<ConnectedRoute>());
			components.Add(ComponentType.ReadWrite<BoardingVehicle>());
			components.Add(ComponentType.ReadWrite<FerryStop>());
			break;
		case TransportType.Post:
		case TransportType.Work:
			break;
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		RouteConnectionData componentData = default(RouteConnectionData);
		componentData.m_AccessConnectionType = m_AccessConnectionType;
		componentData.m_RouteConnectionType = m_RouteConnectionType;
		componentData.m_AccessTrackType = m_AccessTrackType;
		componentData.m_RouteTrackType = m_RouteTrackType;
		componentData.m_AccessRoadType = m_AccessRoadType;
		componentData.m_RouteRoadType = m_RouteRoadType;
		componentData.m_RouteSizeClass = SizeClass.Undefined;
		componentData.m_StartLaneOffset = m_EnterDistance;
		componentData.m_EndMargin = m_ExitDistance;
		TransportStopData componentData2 = new TransportStopData
		{
			m_ComfortFactor = m_ComfortFactor,
			m_LoadingFactor = m_LoadingFactor,
			m_AccessDistance = m_AccessDistance,
			m_BoardingTime = m_BoardingTime,
			m_TransportType = m_TransportType,
			m_PassengerTransport = m_PassengerTransport,
			m_CargoTransport = m_CargoTransport
		};
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, componentData2);
	}
}
