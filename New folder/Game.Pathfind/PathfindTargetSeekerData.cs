using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace Game.Pathfind;

public struct PathfindTargetSeekerData
{
	[ReadOnly]
	public AirwayHelpers.AirwayData m_AirwayData;

	[ReadOnly]
	public ComponentLookup<Owner> m_Owner;

	[ReadOnly]
	public ComponentLookup<Transform> m_Transform;

	[ReadOnly]
	public ComponentLookup<Attached> m_Attached;

	[ReadOnly]
	public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocation;

	[ReadOnly]
	public ComponentLookup<Stopped> m_Stopped;

	[ReadOnly]
	public ComponentLookup<HumanCurrentLane> m_HumanCurrentLane;

	[ReadOnly]
	public ComponentLookup<CarCurrentLane> m_CarCurrentLane;

	[ReadOnly]
	public ComponentLookup<TrainCurrentLane> m_TrainCurrentLane;

	[ReadOnly]
	public ComponentLookup<WatercraftCurrentLane> m_WatercraftCurrentLane;

	[ReadOnly]
	public ComponentLookup<AircraftCurrentLane> m_AircraftCurrentLane;

	[ReadOnly]
	public ComponentLookup<ParkedCar> m_ParkedCar;

	[ReadOnly]
	public ComponentLookup<ParkedTrain> m_ParkedTrain;

	[ReadOnly]
	public ComponentLookup<Train> m_Train;

	[ReadOnly]
	public ComponentLookup<Airplane> m_Airplane;

	[ReadOnly]
	public ComponentLookup<Building> m_Building;

	[ReadOnly]
	public ComponentLookup<PropertyRenter> m_PropertyRenter;

	[ReadOnly]
	public ComponentLookup<CurrentBuilding> m_CurrentBuilding;

	[ReadOnly]
	public ComponentLookup<CurrentTransport> m_CurrentTransport;

	[ReadOnly]
	public ComponentLookup<Curve> m_Curve;

	[ReadOnly]
	public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLane;

	[ReadOnly]
	public ComponentLookup<Game.Net.ParkingLane> m_ParkingLane;

	[ReadOnly]
	public ComponentLookup<Game.Net.CarLane> m_CarLane;

	[ReadOnly]
	public ComponentLookup<MasterLane> m_MasterLane;

	[ReadOnly]
	public ComponentLookup<SlaveLane> m_SlaveLane;

	[ReadOnly]
	public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLane;

	[ReadOnly]
	public ComponentLookup<NodeLane> m_NodeLane;

	[ReadOnly]
	public ComponentLookup<LaneConnection> m_LaneConnection;

	[ReadOnly]
	public ComponentLookup<RouteLane> m_RouteLane;

	[ReadOnly]
	public ComponentLookup<AccessLane> m_AccessLane;

	[ReadOnly]
	public ComponentLookup<PrefabRef> m_PrefabRef;

	[ReadOnly]
	public ComponentLookup<BuildingData> m_BuildingData;

	[ReadOnly]
	public ComponentLookup<PathfindCarData> m_CarPathfindData;

	[ReadOnly]
	public ComponentLookup<SpawnLocationData> m_SpawnLocationData;

	[ReadOnly]
	public ComponentLookup<NetLaneData> m_NetLaneData;

	[ReadOnly]
	public ComponentLookup<CarLaneData> m_CarLaneData;

	[ReadOnly]
	public ComponentLookup<ParkingLaneData> m_ParkingLaneData;

	[ReadOnly]
	public ComponentLookup<TrackLaneData> m_TrackLaneData;

	[ReadOnly]
	public BufferLookup<Game.Net.SubLane> m_SubLane;

	[ReadOnly]
	public BufferLookup<Game.Areas.Node> m_AreaNode;

	[ReadOnly]
	public BufferLookup<Triangle> m_AreaTriangle;

	[ReadOnly]
	public BufferLookup<SpawnLocationElement> m_SpawnLocations;

	[ReadOnly]
	public BufferLookup<LayoutElement> m_VehicleLayout;

	[ReadOnly]
	public BufferLookup<CarNavigationLane> m_CarNavigationLanes;

	[ReadOnly]
	public BufferLookup<WatercraftNavigationLane> m_WatercraftNavigationLanes;

	[ReadOnly]
	public BufferLookup<AircraftNavigationLane> m_AircraftNavigationLanes;

	public PathfindTargetSeekerData(SystemBase system)
	{
		m_AirwayData = default(AirwayHelpers.AirwayData);
		m_Owner = system.GetComponentLookup<Owner>(isReadOnly: true);
		m_Transform = system.GetComponentLookup<Transform>(isReadOnly: true);
		m_Attached = system.GetComponentLookup<Attached>(isReadOnly: true);
		m_SpawnLocation = system.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
		m_Stopped = system.GetComponentLookup<Stopped>(isReadOnly: true);
		m_HumanCurrentLane = system.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
		m_CarCurrentLane = system.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
		m_TrainCurrentLane = system.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
		m_WatercraftCurrentLane = system.GetComponentLookup<WatercraftCurrentLane>(isReadOnly: true);
		m_AircraftCurrentLane = system.GetComponentLookup<AircraftCurrentLane>(isReadOnly: true);
		m_ParkedCar = system.GetComponentLookup<ParkedCar>(isReadOnly: true);
		m_ParkedTrain = system.GetComponentLookup<ParkedTrain>(isReadOnly: true);
		m_Train = system.GetComponentLookup<Train>(isReadOnly: true);
		m_Airplane = system.GetComponentLookup<Airplane>(isReadOnly: true);
		m_Building = system.GetComponentLookup<Building>(isReadOnly: true);
		m_PropertyRenter = system.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		m_CurrentBuilding = system.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
		m_CurrentTransport = system.GetComponentLookup<CurrentTransport>(isReadOnly: true);
		m_Curve = system.GetComponentLookup<Curve>(isReadOnly: true);
		m_PedestrianLane = system.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
		m_ParkingLane = system.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
		m_CarLane = system.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
		m_MasterLane = system.GetComponentLookup<MasterLane>(isReadOnly: true);
		m_SlaveLane = system.GetComponentLookup<SlaveLane>(isReadOnly: true);
		m_ConnectionLane = system.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
		m_NodeLane = system.GetComponentLookup<NodeLane>(isReadOnly: true);
		m_LaneConnection = system.GetComponentLookup<LaneConnection>(isReadOnly: true);
		m_RouteLane = system.GetComponentLookup<RouteLane>(isReadOnly: true);
		m_AccessLane = system.GetComponentLookup<AccessLane>(isReadOnly: true);
		m_PrefabRef = system.GetComponentLookup<PrefabRef>(isReadOnly: true);
		m_BuildingData = system.GetComponentLookup<BuildingData>(isReadOnly: true);
		m_CarPathfindData = system.GetComponentLookup<PathfindCarData>(isReadOnly: true);
		m_SpawnLocationData = system.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
		m_NetLaneData = system.GetComponentLookup<NetLaneData>(isReadOnly: true);
		m_CarLaneData = system.GetComponentLookup<CarLaneData>(isReadOnly: true);
		m_ParkingLaneData = system.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
		m_TrackLaneData = system.GetComponentLookup<TrackLaneData>(isReadOnly: true);
		m_SubLane = system.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
		m_AreaNode = system.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
		m_AreaTriangle = system.GetBufferLookup<Triangle>(isReadOnly: true);
		m_SpawnLocations = system.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
		m_VehicleLayout = system.GetBufferLookup<LayoutElement>(isReadOnly: true);
		m_CarNavigationLanes = system.GetBufferLookup<CarNavigationLane>(isReadOnly: true);
		m_WatercraftNavigationLanes = system.GetBufferLookup<WatercraftNavigationLane>(isReadOnly: true);
		m_AircraftNavigationLanes = system.GetBufferLookup<AircraftNavigationLane>(isReadOnly: true);
	}

	public void Update(SystemBase system, AirwayHelpers.AirwayData airwayData)
	{
		m_AirwayData = airwayData;
		m_Owner.Update(system);
		m_Transform.Update(system);
		m_Attached.Update(system);
		m_SpawnLocation.Update(system);
		m_Stopped.Update(system);
		m_HumanCurrentLane.Update(system);
		m_CarCurrentLane.Update(system);
		m_TrainCurrentLane.Update(system);
		m_WatercraftCurrentLane.Update(system);
		m_AircraftCurrentLane.Update(system);
		m_ParkedCar.Update(system);
		m_ParkedTrain.Update(system);
		m_Train.Update(system);
		m_Airplane.Update(system);
		m_Building.Update(system);
		m_PropertyRenter.Update(system);
		m_CurrentBuilding.Update(system);
		m_CurrentTransport.Update(system);
		m_Curve.Update(system);
		m_PedestrianLane.Update(system);
		m_ParkingLane.Update(system);
		m_CarLane.Update(system);
		m_MasterLane.Update(system);
		m_SlaveLane.Update(system);
		m_ConnectionLane.Update(system);
		m_NodeLane.Update(system);
		m_LaneConnection.Update(system);
		m_RouteLane.Update(system);
		m_AccessLane.Update(system);
		m_PrefabRef.Update(system);
		m_BuildingData.Update(system);
		m_CarPathfindData.Update(system);
		m_SpawnLocationData.Update(system);
		m_NetLaneData.Update(system);
		m_CarLaneData.Update(system);
		m_ParkingLaneData.Update(system);
		m_TrackLaneData.Update(system);
		m_SubLane.Update(system);
		m_AreaNode.Update(system);
		m_AreaTriangle.Update(system);
		m_SpawnLocations.Update(system);
		m_VehicleLayout.Update(system);
		m_CarNavigationLanes.Update(system);
		m_WatercraftNavigationLanes.Update(system);
		m_AircraftNavigationLanes.Update(system);
	}
}
