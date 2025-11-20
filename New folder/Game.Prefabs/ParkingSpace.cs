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
public class ParkingSpace : ComponentBase
{
	public RoadTypes m_RoadType = RoadTypes.Bicycle;

	public float m_ComfortFactor;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ParkingSpaceData>());
		components.Add(ComponentType.ReadWrite<TransportStopData>());
		components.Add(ComponentType.ReadWrite<RouteConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.Color>());
		components.Add(ComponentType.ReadWrite<Game.Routes.TransportStop>());
		components.Add(ComponentType.ReadWrite<Game.Routes.ParkingSpace>());
		switch (m_RoadType)
		{
		case RoadTypes.Car:
			components.Add(ComponentType.ReadWrite<CarParking>());
			components.Add(ComponentType.ReadWrite<CurrentDistrict>());
			break;
		case RoadTypes.Bicycle:
			components.Add(ComponentType.ReadWrite<BicycleParking>());
			break;
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (!base.prefab.Has<TransportStop>())
		{
			RouteConnectionData componentData = default(RouteConnectionData);
			componentData.m_AccessConnectionType = RouteConnectionType.Pedestrian;
			componentData.m_RouteConnectionType = RouteConnectionType.Road;
			componentData.m_AccessTrackType = TrackTypes.None;
			componentData.m_RouteTrackType = TrackTypes.None;
			componentData.m_AccessRoadType = RoadTypes.None;
			componentData.m_RouteRoadType = m_RoadType;
			componentData.m_RouteSizeClass = SizeClass.Undefined;
			componentData.m_StartLaneOffset = 0f;
			componentData.m_EndMargin = 0f;
			TransportStopData componentData2 = default(TransportStopData);
			componentData2.m_ComfortFactor = m_ComfortFactor;
			componentData2.m_LoadingFactor = 0f;
			componentData2.m_AccessDistance = 0f;
			componentData2.m_BoardingTime = 0f;
			componentData2.m_TransportType = TransportType.None;
			componentData2.m_PassengerTransport = true;
			componentData2.m_CargoTransport = false;
			switch (m_RoadType)
			{
			case RoadTypes.Car:
				componentData2.m_TransportType = TransportType.Car;
				break;
			case RoadTypes.Bicycle:
				componentData2.m_TransportType = TransportType.Bicycle;
				break;
			}
			entityManager.SetComponentData(entity, componentData);
			entityManager.SetComponentData(entity, componentData2);
		}
	}
}
