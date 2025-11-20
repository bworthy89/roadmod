using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { typeof(ObjectPrefab) })]
public class MailBox : ComponentBase
{
	public int m_MailCapacity = 1000;

	public float m_ComfortFactor;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MailBoxData>());
		components.Add(ComponentType.ReadWrite<TransportStopData>());
		components.Add(ComponentType.ReadWrite<RouteConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.Color>());
		components.Add(ComponentType.ReadWrite<Game.Routes.TransportStop>());
		components.Add(ComponentType.ReadWrite<Game.Routes.MailBox>());
		components.Add(ComponentType.ReadWrite<AccessLane>());
		components.Add(ComponentType.ReadWrite<RouteLane>());
		components.Add(ComponentType.ReadWrite<CurrentDistrict>());
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
		componentData.m_RouteRoadType = RoadTypes.Car;
		componentData.m_RouteSizeClass = SizeClass.Undefined;
		componentData.m_StartLaneOffset = 0f;
		componentData.m_EndMargin = 0f;
		TransportStopData componentData2 = default(TransportStopData);
		componentData2.m_ComfortFactor = m_ComfortFactor;
		componentData2.m_LoadingFactor = 0f;
		componentData2.m_AccessDistance = 0f;
		componentData2.m_BoardingTime = 0f;
		componentData2.m_TransportType = TransportType.Post;
		componentData2.m_PassengerTransport = false;
		componentData2.m_CargoTransport = true;
		MailBoxData componentData3 = default(MailBoxData);
		componentData3.m_MailCapacity = m_MailCapacity;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, componentData2);
		entityManager.SetComponentData(entity, componentData3);
	}
}
