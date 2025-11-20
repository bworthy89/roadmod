using Colossal.Mathematics;
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
using Unity.Mathematics;
using UnityEngine;

namespace Game.Pathfind;

public struct PathfindTargetSeeker<TBuffer> where TBuffer : IPathfindTargetBuffer
{
	public PathfindParameters m_PathfindParameters;

	public bool m_IsStartTarget;

	public SetupQueueTarget m_SetupQueueTarget;

	public TBuffer m_Buffer;

	[ReadOnly]
	public RandomSeed m_RandomSeed;

	[ReadOnly]
	public AirwayHelpers.AirwayData m_AirwayData;

	[ReadOnly]
	public ComponentLookup<Owner> m_Owner;

	[ReadOnly]
	public ComponentLookup<Game.Objects.Transform> m_Transform;

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

	public PathfindTargetSeeker(PathfindTargetSeekerData data, PathfindParameters pathfindParameters, SetupQueueTarget setupQueueTarget, TBuffer buffer, RandomSeed randomSeed, bool isStartTarget)
	{
		m_PathfindParameters = pathfindParameters;
		m_IsStartTarget = isStartTarget;
		m_SetupQueueTarget = setupQueueTarget;
		m_Buffer = buffer;
		m_RandomSeed = randomSeed;
		m_AirwayData = data.m_AirwayData;
		m_Owner = data.m_Owner;
		m_Transform = data.m_Transform;
		m_Attached = data.m_Attached;
		m_SpawnLocation = data.m_SpawnLocation;
		m_Stopped = data.m_Stopped;
		m_HumanCurrentLane = data.m_HumanCurrentLane;
		m_CarCurrentLane = data.m_CarCurrentLane;
		m_TrainCurrentLane = data.m_TrainCurrentLane;
		m_WatercraftCurrentLane = data.m_WatercraftCurrentLane;
		m_AircraftCurrentLane = data.m_AircraftCurrentLane;
		m_ParkedCar = data.m_ParkedCar;
		m_ParkedTrain = data.m_ParkedTrain;
		m_Train = data.m_Train;
		m_Airplane = data.m_Airplane;
		m_Building = data.m_Building;
		m_PropertyRenter = data.m_PropertyRenter;
		m_CurrentBuilding = data.m_CurrentBuilding;
		m_CurrentTransport = data.m_CurrentTransport;
		m_Curve = data.m_Curve;
		m_PedestrianLane = data.m_PedestrianLane;
		m_ParkingLane = data.m_ParkingLane;
		m_CarLane = data.m_CarLane;
		m_MasterLane = data.m_MasterLane;
		m_SlaveLane = data.m_SlaveLane;
		m_ConnectionLane = data.m_ConnectionLane;
		m_NodeLane = data.m_NodeLane;
		m_LaneConnection = data.m_LaneConnection;
		m_RouteLane = data.m_RouteLane;
		m_AccessLane = data.m_AccessLane;
		m_PrefabRef = data.m_PrefabRef;
		m_BuildingData = data.m_BuildingData;
		m_CarPathfindData = data.m_CarPathfindData;
		m_SpawnLocationData = data.m_SpawnLocationData;
		m_NetLaneData = data.m_NetLaneData;
		m_CarLaneData = data.m_CarLaneData;
		m_ParkingLaneData = data.m_ParkingLaneData;
		m_TrackLaneData = data.m_TrackLaneData;
		m_SubLane = data.m_SubLane;
		m_AreaNode = data.m_AreaNode;
		m_AreaTriangle = data.m_AreaTriangle;
		m_SpawnLocations = data.m_SpawnLocations;
		m_VehicleLayout = data.m_VehicleLayout;
		m_CarNavigationLanes = data.m_CarNavigationLanes;
		m_WatercraftNavigationLanes = data.m_WatercraftNavigationLanes;
		m_AircraftNavigationLanes = data.m_AircraftNavigationLanes;
	}

	public void AddTarget(ref Unity.Mathematics.Random random, Entity target, Entity entity, float delta, float cost, EdgeFlags flags)
	{
		cost += random.NextFloat(m_SetupQueueTarget.m_RandomCost);
		ref TBuffer buffer = ref m_Buffer;
		PathTarget pathTarget = new PathTarget(target, entity, delta, cost, flags);
		buffer.Enqueue(pathTarget);
	}

	public int FindTargets(Entity entity, float cost)
	{
		return FindTargets(entity, entity, cost, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
	}

	public int FindTargets(Entity target, Entity entity, float cost, EdgeFlags flags, bool allowAccessRestriction, bool navigationEnd)
	{
		Unity.Mathematics.Random random = m_RandomSeed.GetRandom(entity.Index);
		int num = 0;
		if ((m_PathfindParameters.m_PathfindFlags & PathfindFlags.SkipPathfind) != 0)
		{
			AddTarget(ref random, target, entity, 0f, cost, flags);
			return 1;
		}
		if (m_CurrentTransport.HasComponent(entity))
		{
			entity = m_CurrentTransport[entity].m_CurrentTransport;
		}
		if (m_HumanCurrentLane.HasComponent(entity))
		{
			HumanCurrentLane humanCurrentLane = m_HumanCurrentLane[entity];
			if (m_Curve.HasComponent(humanCurrentLane.m_Lane))
			{
				if ((m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0)
				{
					return num + AddPedestrianLaneTargets(ref random, target, humanCurrentLane.m_Lane, humanCurrentLane.m_CurvePosition.y, cost, 0f, flags, allowAccessRestriction);
				}
				float3 comparePosition = MathUtils.Position(m_Curve[humanCurrentLane.m_Lane].m_Bezier, humanCurrentLane.m_CurvePosition.y);
				if (GetEdge(humanCurrentLane.m_Lane, out var edge))
				{
					return num + AddEdgeTargets(ref random, target, cost, flags, edge, comparePosition, 0f, allowLaneGroupSwitch: false, allowAccessRestriction);
				}
				entity = humanCurrentLane.m_Lane;
			}
			else if (m_SpawnLocation.HasComponent(humanCurrentLane.m_Lane))
			{
				num += AddSpawnLocation(ref random, target, humanCurrentLane.m_Lane, cost, flags, ignoreActivityMask: true, allowAccessRestriction);
				if (num != 0)
				{
					return num;
				}
				entity = humanCurrentLane.m_Lane;
			}
			else
			{
				entity = humanCurrentLane.m_Lane;
			}
		}
		if (m_CarCurrentLane.HasComponent(entity))
		{
			GetCarLane(entity, navigationEnd, out var lane, out var curvePos, out var flags2);
			if (m_Curve.HasComponent(lane))
			{
				float3 comparePosition2 = MathUtils.Position(m_Curve[lane].m_Bezier, curvePos);
				if ((m_SetupQueueTarget.m_Methods & (PathMethod.Road | PathMethod.Bicycle)) != 0 && (m_SetupQueueTarget.m_RoadTypes & (RoadTypes.Car | RoadTypes.Bicycle)) != RoadTypes.None)
				{
					return num + AddCarLaneTargets(ref random, target, lane, comparePosition2, 0f, curvePos, cost, flags, (flags2 & (Game.Vehicles.CarLaneFlags.EnteringRoad | Game.Vehicles.CarLaneFlags.IsBlocked)) != 0, m_Stopped.HasComponent(entity), allowAccessRestriction);
				}
				if (GetEdge(lane, out var edge2))
				{
					return num + AddEdgeTargets(ref random, target, cost, flags, edge2, comparePosition2, 0f, allowLaneGroupSwitch: false, allowAccessRestriction);
				}
				entity = lane;
			}
			else if (m_SpawnLocation.HasComponent(lane))
			{
				num += AddSpawnLocation(ref random, target, lane, cost, flags, ignoreActivityMask: true, allowAccessRestriction);
				if (num != 0)
				{
					return num;
				}
				entity = lane;
			}
			else
			{
				entity = lane;
			}
		}
		if (m_ParkedCar.TryGetComponent(entity, out var componentData))
		{
			if (m_Curve.TryGetComponent(componentData.m_Lane, out var componentData2))
			{
				if ((m_SetupQueueTarget.m_Methods & (PathMethod.Parking | PathMethod.SpecialParking | PathMethod.BicycleParking)) != 0)
				{
					return num + AddParkingLaneTargets(ref random, target, componentData.m_Lane, componentData.m_CurvePosition, cost, flags, allowAccessRestriction);
				}
				float3 comparePosition3 = MathUtils.Position(componentData2.m_Bezier, componentData.m_CurvePosition);
				if (m_LaneConnection.TryGetComponent(componentData.m_Lane, out var componentData3))
				{
					return num + AddLaneConnectionTargets(ref random, target, cost, flags, componentData3, comparePosition3, 0f, allowLaneGroupSwitch: true, allowAccessRestriction);
				}
				if (GetEdge(componentData.m_Lane, out var edge3))
				{
					return num + AddEdgeTargets(ref random, target, cost, flags, edge3, comparePosition3, 0f, allowLaneGroupSwitch: false, allowAccessRestriction);
				}
				entity = componentData.m_Lane;
			}
			else if (m_SpawnLocation.HasComponent(componentData.m_Lane))
			{
				num += AddSpawnLocation(ref random, target, componentData.m_Lane, cost, flags, ignoreActivityMask: false, allowAccessRestriction);
				if (num != 0)
				{
					return num;
				}
				entity = componentData.m_Lane;
			}
			else
			{
				entity = componentData.m_Lane;
			}
		}
		if (m_WatercraftCurrentLane.HasComponent(entity))
		{
			GetWatercraftLane(entity, navigationEnd, out var lane2, out var curvePos2, out var flags3);
			if (m_Curve.HasComponent(lane2))
			{
				float3 comparePosition4 = MathUtils.Position(m_Curve[lane2].m_Bezier, curvePos2);
				return num + AddCarLaneTargets(ref random, target, lane2, comparePosition4, 0f, curvePos2, cost, flags, (flags3 & WatercraftLaneFlags.IsBlocked) != 0, m_Stopped.HasComponent(entity), allowAccessRestriction);
			}
			entity = lane2;
		}
		if (m_AircraftCurrentLane.HasComponent(entity))
		{
			GetAircraftLane(entity, navigationEnd, out var lane3, out var curvePos3, out var flags4);
			if ((flags4 & (AircraftLaneFlags.TransformTarget | AircraftLaneFlags.Flying)) != 0)
			{
				if (m_Transform.HasComponent(lane3))
				{
					float3 position = m_Transform[lane3].m_Position;
					AirwayHelpers.AirwayMap airwayMap = (m_Airplane.HasComponent(entity) ? m_AirwayData.airplaneMap : m_AirwayData.helicopterMap);
					lane3 = Entity.Null;
					float distance = float.MaxValue;
					airwayMap.FindClosestLane(position, m_Curve, ref lane3, ref curvePos3, ref distance);
					if (lane3 != Entity.Null)
					{
						AddTarget(ref random, entity, lane3, curvePos3, cost, ~(EdgeFlags.DefaultMask | EdgeFlags.Secondary));
						num++;
					}
				}
				else if (m_Curve.HasComponent(lane3))
				{
					AddTarget(ref random, entity, lane3, curvePos3, cost, flags);
					num++;
				}
				return num;
			}
			if (m_Curve.HasComponent(lane3))
			{
				float3 comparePosition5 = MathUtils.Position(m_Curve[lane3].m_Bezier, curvePos3);
				return num + AddCarLaneTargets(ref random, target, lane3, comparePosition5, 0f, curvePos3, cost, flags, allowLaneGroupSwitch: true, m_Stopped.HasComponent(entity), allowAccessRestriction);
			}
			entity = lane3;
		}
		if (m_Train.HasComponent(entity))
		{
			return num + AddTrainTargets(ref random, target, entity, cost, flags);
		}
		bool flag = false;
		if (m_RouteLane.HasComponent(entity))
		{
			RouteLane routeLane = m_RouteLane[entity];
			if (m_IsStartTarget)
			{
				if (routeLane.m_EndLane != Entity.Null)
				{
					num += AddLaneTarget(ref random, target, Entity.Null, routeLane.m_EndLane, routeLane.m_EndCurvePos, cost, flags, allowAccessRestriction);
				}
			}
			else if (routeLane.m_StartLane != Entity.Null)
			{
				num += AddLaneTarget(ref random, target, Entity.Null, routeLane.m_StartLane, routeLane.m_StartCurvePos, cost, flags, allowAccessRestriction);
			}
			flag = true;
		}
		if (m_AccessLane.HasComponent(entity))
		{
			AccessLane accessLane = m_AccessLane[entity];
			if (accessLane.m_Lane != Entity.Null)
			{
				num = ((!m_SpawnLocation.HasComponent(accessLane.m_Lane)) ? (num + AddLaneTarget(ref random, target, Entity.Null, accessLane.m_Lane, accessLane.m_CurvePos, cost, flags, allowAccessRestriction)) : (num + AddSpawnLocation(ref random, target, accessLane.m_Lane, cost, flags, ignoreActivityMask: true, allowAccessRestriction)));
			}
			flag = true;
		}
		if (flag && num != 0)
		{
			return num;
		}
		if (m_Attached.HasComponent(entity) && !m_Building.HasComponent(entity))
		{
			Attached attached = m_Attached[entity];
			if (m_SubLane.HasBuffer(attached.m_Parent))
			{
				Game.Objects.Transform transform = m_Transform[entity];
				return num + AddEdgeTargets(ref random, target, cost, flags, attached.m_Parent, transform.m_Position, 0f, allowLaneGroupSwitch: false, allowAccessRestriction);
			}
		}
		Entity entity2 = (m_PropertyRenter.HasComponent(entity) ? m_PropertyRenter[entity].m_Property : ((!m_CurrentBuilding.HasComponent(entity)) ? entity : m_CurrentBuilding[entity].m_CurrentBuilding));
		while (!m_SpawnLocations.HasBuffer(entity2))
		{
			if (m_SubLane.HasBuffer(entity2))
			{
				num += AddSubLaneTargets(ref random, target, entity2, cost, randomCurvePos: false, allowAccessRestriction: false, flags);
			}
			if (m_Owner.HasComponent(entity2))
			{
				entity2 = m_Owner[entity2].m_Owner;
				continue;
			}
			return num;
		}
		bool addFrontConnection = num == 0;
		if ((m_PathfindParameters.m_PathfindFlags & PathfindFlags.Simplified) == 0 && m_SpawnLocations.HasBuffer(entity2))
		{
			DynamicBuffer<SpawnLocationElement> dynamicBuffer = m_SpawnLocations[entity2];
			int num2 = 0;
			if (m_SetupQueueTarget.m_RandomCost != 0f)
			{
				int num3 = 0;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity spawnLocation = dynamicBuffer[i].m_SpawnLocation;
					num3 = ((dynamicBuffer[i].m_Type != SpawnLocationType.ParkingLane) ? (num3 + AddSpawnLocation(ref random, target, spawnLocation, cost, flags, ignoreActivityMask: false, allowAccessRestriction, countOnly: true, ignoreParked: false, ref addFrontConnection)) : (num3 + AddParkingLane(ref random, target, spawnLocation, cost, flags, allowAccessRestriction, countOnly: true, ref addFrontConnection)));
				}
				num2 = random.NextInt(num3);
			}
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				Entity spawnLocation2 = dynamicBuffer[j].m_SpawnLocation;
				float cost2 = math.select(cost, cost + m_SetupQueueTarget.m_RandomCost, num2 != 0);
				int num4 = ((dynamicBuffer[j].m_Type != SpawnLocationType.ParkingLane) ? AddSpawnLocation(ref random, target, spawnLocation2, cost2, flags, ignoreActivityMask: false, allowAccessRestriction, countOnly: false, ignoreParked: false, ref addFrontConnection) : AddParkingLane(ref random, target, spawnLocation2, cost, flags, allowAccessRestriction, countOnly: false, ref addFrontConnection));
				num += num4;
				num2 -= num4;
			}
		}
		if (addFrontConnection && m_Building.TryGetComponent(entity2, out var componentData4))
		{
			PrefabRef prefabRef = m_PrefabRef[entity2];
			Game.Objects.Transform transform2 = m_Transform[entity2];
			if (componentData4.m_RoadEdge != Entity.Null)
			{
				BuildingData buildingData = m_BuildingData[prefabRef.m_Prefab];
				float3 comparePosition6 = transform2.m_Position;
				if (!m_Owner.TryGetComponent(componentData4.m_RoadEdge, out var componentData5) || componentData5.m_Owner != entity2)
				{
					comparePosition6 = BuildingUtils.CalculateFrontPosition(transform2, buildingData.m_LotSize.y);
				}
				num += AddEdgeTargets(ref random, target, cost, flags, componentData4.m_RoadEdge, comparePosition6, 0f, allowLaneGroupSwitch: true, allowAccessRestriction: false);
			}
		}
		return num;
	}

	private bool CheckAccessRestriction(bool allowAccessRestriction, Game.Objects.SpawnLocation spawnLocation)
	{
		if (!allowAccessRestriction && !(spawnLocation.m_AccessRestriction == Entity.Null))
		{
			return (spawnLocation.m_Flags & (SpawnLocationFlags.AllowEnter | SpawnLocationFlags.AllowExit)) == SpawnLocationFlags.AllowEnter;
		}
		return true;
	}

	private bool CheckAccessRestriction(bool allowAccessRestriction, Game.Net.PedestrianLane pedestrianLane)
	{
		if (!allowAccessRestriction && !(pedestrianLane.m_AccessRestriction == Entity.Null))
		{
			return (pedestrianLane.m_Flags & (PedestrianLaneFlags.AllowEnter | PedestrianLaneFlags.AllowExit)) == PedestrianLaneFlags.AllowEnter;
		}
		return true;
	}

	private bool CheckAccessRestriction(bool allowAccessRestriction, Game.Net.CarLane carLane)
	{
		if (!allowAccessRestriction && !(carLane.m_AccessRestriction == Entity.Null))
		{
			return ((uint)carLane.m_Flags & 0x80000000u) != 0;
		}
		return true;
	}

	private bool CheckAccessRestriction(bool allowAccessRestriction, Game.Net.ParkingLane parkingLane)
	{
		if (!allowAccessRestriction && !(parkingLane.m_AccessRestriction == Entity.Null))
		{
			return (parkingLane.m_Flags & (ParkingLaneFlags.AllowEnter | ParkingLaneFlags.AllowExit)) == ParkingLaneFlags.AllowEnter;
		}
		return true;
	}

	private bool CheckAccessRestriction(bool allowAccessRestriction, Game.Net.ConnectionLane connectionLane)
	{
		if (!allowAccessRestriction && !(connectionLane.m_AccessRestriction == Entity.Null))
		{
			return (connectionLane.m_Flags & (ConnectionLaneFlags.AllowEnter | ConnectionLaneFlags.AllowExit)) == ConnectionLaneFlags.AllowEnter;
		}
		return true;
	}

	private int AddParkingLane(ref Unity.Mathematics.Random random, Entity target, Entity parkingLaneEntity, float cost, EdgeFlags flags, bool allowAccessRestriction, bool countOnly, ref bool addFrontConnection)
	{
		if ((m_SetupQueueTarget.m_Methods & (PathMethod.Parking | PathMethod.SpecialParking | PathMethod.BicycleParking)) == 0)
		{
			return 0;
		}
		PrefabRef prefabRef = m_PrefabRef[parkingLaneEntity];
		if (!m_ParkingLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
		{
			return 0;
		}
		if ((m_SetupQueueTarget.m_RoadTypes & componentData.m_RoadTypes) == 0)
		{
			return 0;
		}
		Game.Net.ParkingLane parkingLane = m_ParkingLane[parkingLaneEntity];
		PathMethod pathMethod = ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
		if ((parkingLane.m_Flags & ParkingLaneFlags.SpecialVehicles) != 0)
		{
			pathMethod |= PathMethod.SpecialParking;
		}
		else
		{
			if ((componentData.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
			{
				pathMethod |= PathMethod.Parking;
			}
			if ((componentData.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
			{
				pathMethod |= PathMethod.BicycleParking;
			}
		}
		float x = VehicleUtils.GetParkingSize(componentData).x;
		float y = math.max(1f, parkingLane.m_FreeSpace);
		if ((m_SetupQueueTarget.m_Methods & pathMethod) == 0)
		{
			return 0;
		}
		if (math.any(m_PathfindParameters.m_ParkingSize > new float2(x, y)))
		{
			return 0;
		}
		if (!CheckAccessRestriction(allowAccessRestriction, parkingLane))
		{
			return 0;
		}
		if (!countOnly)
		{
			AddTarget(ref random, target, parkingLaneEntity, 0.5f, cost, flags);
			addFrontConnection = false;
		}
		return 1;
	}

	private int AddSpawnLocation(ref Unity.Mathematics.Random random, Entity target, Entity spawnLocationEntity, float cost, EdgeFlags flags, bool ignoreActivityMask, bool allowAccessRestriction)
	{
		bool addFrontConnection = false;
		return AddSpawnLocation(ref random, target, spawnLocationEntity, cost, flags, ignoreActivityMask, allowAccessRestriction, countOnly: false, ignoreParked: true, ref addFrontConnection);
	}

	private int AddSpawnLocation(ref Unity.Mathematics.Random random, Entity target, Entity spawnLocationEntity, float cost, EdgeFlags flags, bool ignoreActivityMask, bool allowAccessRestriction, bool countOnly, bool ignoreParked, ref bool addFrontConnection)
	{
		PrefabRef prefabRef = m_PrefabRef[spawnLocationEntity];
		if (!m_SpawnLocationData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
		{
			return 0;
		}
		bool flag = false;
		switch (componentData.m_ConnectionType)
		{
		case RouteConnectionType.Pedestrian:
			if ((m_SetupQueueTarget.m_Methods & (PathMethod.Pedestrian | PathMethod.Bicycle)) == 0)
			{
				return 0;
			}
			break;
		case RouteConnectionType.Cargo:
			if ((m_SetupQueueTarget.m_Methods & PathMethod.CargoLoading) == 0 || (m_SetupQueueTarget.m_RoadTypes & componentData.m_RoadTypes) == 0)
			{
				return 0;
			}
			if ((m_PathfindParameters.m_Methods & PathMethod.CargoLoading) == 0)
			{
				flag = true;
			}
			break;
		case RouteConnectionType.Road:
			if ((m_SetupQueueTarget.m_Methods & PathMethod.Road) == 0 || (m_SetupQueueTarget.m_RoadTypes & componentData.m_RoadTypes) == 0)
			{
				return 0;
			}
			if (!ignoreParked && (m_SetupQueueTarget.m_Methods & PathMethod.SpecialParking) != 0)
			{
				cost += m_SetupQueueTarget.m_RandomCost;
			}
			break;
		case RouteConnectionType.Air:
			if ((m_SetupQueueTarget.m_Methods & PathMethod.Road) == 0 || (m_SetupQueueTarget.m_RoadTypes & componentData.m_RoadTypes) == 0)
			{
				return 0;
			}
			break;
		case RouteConnectionType.Track:
			if ((m_SetupQueueTarget.m_Methods & PathMethod.Track) == 0 || (m_SetupQueueTarget.m_TrackTypes & componentData.m_TrackTypes) == 0)
			{
				return 0;
			}
			break;
		case RouteConnectionType.Parking:
			if ((componentData.m_RoadTypes != RoadTypes.Bicycle || (m_SetupQueueTarget.m_Methods & (PathMethod.Pedestrian | PathMethod.Bicycle)) == 0) && ((m_SetupQueueTarget.m_Methods & PathMethod.Parking) == 0 || (m_SetupQueueTarget.m_RoadTypes & componentData.m_RoadTypes) == 0))
			{
				return 0;
			}
			break;
		case RouteConnectionType.Offroad:
			if ((m_SetupQueueTarget.m_Methods & PathMethod.Offroad) == 0 || (m_SetupQueueTarget.m_RoadTypes & componentData.m_RoadTypes) == 0)
			{
				return 0;
			}
			break;
		default:
			return 0;
		}
		if (!ignoreActivityMask && componentData.m_ActivityMask.m_Mask != 0 && (componentData.m_ActivityMask.m_Mask & m_SetupQueueTarget.m_ActivityMask.m_Mask) == 0)
		{
			return 0;
		}
		int num;
		DynamicBuffer<Game.Net.SubLane> bufferData;
		if (m_SpawnLocation.TryGetComponent(spawnLocationEntity, out var componentData2))
		{
			if (CheckAccessRestriction(allowAccessRestriction, componentData2))
			{
				if (!countOnly)
				{
					cost += math.select(math.max(m_SetupQueueTarget.m_RandomCost * 3f, 30f), 0f, ignoreParked || (componentData2.m_Flags & SpawnLocationFlags.ParkedVehicle) == 0);
					if (flag)
					{
						if (componentData2.m_ConnectedLane1 != Entity.Null)
						{
							AddTarget(ref random, target, componentData2.m_ConnectedLane1, componentData2.m_CurvePosition1, cost, flags);
						}
						if (componentData2.m_ConnectedLane2 != Entity.Null)
						{
							AddTarget(ref random, target, componentData2.m_ConnectedLane2, componentData2.m_CurvePosition2, cost, flags);
						}
						goto IL_033c;
					}
					if (componentData.m_ConnectionType != RouteConnectionType.Pedestrian)
					{
						if (componentData.m_ConnectionType == RouteConnectionType.Parking)
						{
							num = ((componentData.m_RoadTypes == RoadTypes.Bicycle) ? 1 : 0);
							if (num != 0)
							{
								goto IL_02e1;
							}
						}
						else
						{
							num = 0;
						}
						goto IL_02f0;
					}
					num = 1;
					goto IL_02e1;
				}
				goto IL_0353;
			}
		}
		else if (m_SubLane.TryGetBuffer(spawnLocationEntity, out bufferData))
		{
			int num2 = 0;
			int2 @int = new int2(0, bufferData.Length - 1);
			if (bufferData.Length == 0)
			{
				UnityEngine.Debug.Log($"Empty subLanes: {spawnLocationEntity.Index}");
			}
			else if (m_SetupQueueTarget.m_RandomCost != 0f)
			{
				@int = random.NextInt(bufferData.Length);
			}
			for (int i = @int.x; i <= @int.y; i++)
			{
				Entity subLane = bufferData[i].m_SubLane;
				if (m_ConnectionLane.TryGetComponent(subLane, out var componentData3) && CheckAccessRestriction(allowAccessRestriction, componentData3) && (((m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0 && (componentData3.m_Flags & ConnectionLaneFlags.Pedestrian) != 0) || ((m_SetupQueueTarget.m_Methods & PathMethod.Offroad) != 0 && (componentData3.m_Flags & ConnectionLaneFlags.Road) != 0 && (m_SetupQueueTarget.m_RoadTypes & componentData3.m_RoadTypes) != RoadTypes.None)))
				{
					if (!countOnly)
					{
						AddTarget(ref random, target, subLane, 0.5f, cost, flags);
						addFrontConnection = false;
					}
					num2++;
				}
			}
			return num2;
		}
		return 0;
		IL_033c:
		addFrontConnection &= componentData2.m_ConnectedLane1 == Entity.Null;
		goto IL_0353;
		IL_02f0:
		AddTarget(ref random, target, spawnLocationEntity, 1f, cost, flags);
		goto IL_0302;
		IL_02e1:
		if ((m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0)
		{
			goto IL_02f0;
		}
		goto IL_0302;
		IL_0302:
		if (num != 0 && (m_SetupQueueTarget.m_Methods & PathMethod.Bicycle) != 0 && componentData.m_ActivityMask.m_Mask == 0)
		{
			AddTarget(ref random, target, spawnLocationEntity, 1f, cost, flags | EdgeFlags.Secondary);
		}
		goto IL_033c;
		IL_0353:
		return 1;
	}

	private void GetCarLane(Entity entity, bool navigationEnd, out Entity lane, out float curvePos, out Game.Vehicles.CarLaneFlags flags)
	{
		CarCurrentLane carCurrentLane = m_CarCurrentLane[entity];
		lane = carCurrentLane.m_Lane;
		curvePos = math.select(carCurrentLane.m_CurvePosition.y, carCurrentLane.m_CurvePosition.z, navigationEnd || (carCurrentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ClearedForPathfind) != 0);
		flags = carCurrentLane.m_LaneFlags;
		if (!m_CarNavigationLanes.TryGetBuffer(entity, out var bufferData))
		{
			return;
		}
		if (navigationEnd)
		{
			if (bufferData.Length != 0)
			{
				CarNavigationLane carNavigationLane = bufferData[bufferData.Length - 1];
				lane = carNavigationLane.m_Lane;
				curvePos = carNavigationLane.m_CurvePosition.y;
				flags = carNavigationLane.m_Flags;
			}
			return;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			CarNavigationLane carNavigationLane2 = bufferData[i];
			if ((carNavigationLane2.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.ClearedForPathfind)) == 0)
			{
				break;
			}
			lane = carNavigationLane2.m_Lane;
			curvePos = carNavigationLane2.m_CurvePosition.y;
			flags = carNavigationLane2.m_Flags;
		}
	}

	private void GetWatercraftLane(Entity entity, bool navigationEnd, out Entity lane, out float curvePos, out WatercraftLaneFlags flags)
	{
		WatercraftCurrentLane watercraftCurrentLane = m_WatercraftCurrentLane[entity];
		lane = watercraftCurrentLane.m_Lane;
		curvePos = math.select(watercraftCurrentLane.m_CurvePosition.y, watercraftCurrentLane.m_CurvePosition.z, navigationEnd);
		flags = watercraftCurrentLane.m_LaneFlags;
		if (!m_WatercraftNavigationLanes.HasBuffer(entity))
		{
			return;
		}
		DynamicBuffer<WatercraftNavigationLane> dynamicBuffer = m_WatercraftNavigationLanes[entity];
		if (navigationEnd)
		{
			if (dynamicBuffer.Length != 0)
			{
				WatercraftNavigationLane watercraftNavigationLane = dynamicBuffer[dynamicBuffer.Length - 1];
				lane = watercraftNavigationLane.m_Lane;
				curvePos = watercraftNavigationLane.m_CurvePosition.y;
				flags = watercraftNavigationLane.m_Flags;
			}
			return;
		}
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			WatercraftNavigationLane watercraftNavigationLane2 = dynamicBuffer[i];
			if ((watercraftNavigationLane2.m_Flags & WatercraftLaneFlags.Reserved) == 0)
			{
				break;
			}
			lane = watercraftNavigationLane2.m_Lane;
			curvePos = watercraftNavigationLane2.m_CurvePosition.y;
			flags = watercraftNavigationLane2.m_Flags;
		}
	}

	private void GetAircraftLane(Entity entity, bool navigationEnd, out Entity lane, out float curvePos, out AircraftLaneFlags flags)
	{
		AircraftCurrentLane aircraftCurrentLane = m_AircraftCurrentLane[entity];
		lane = aircraftCurrentLane.m_Lane;
		curvePos = math.select(aircraftCurrentLane.m_CurvePosition.y, aircraftCurrentLane.m_CurvePosition.z, navigationEnd);
		flags = aircraftCurrentLane.m_LaneFlags;
		if (!m_AircraftNavigationLanes.HasBuffer(entity))
		{
			return;
		}
		DynamicBuffer<AircraftNavigationLane> dynamicBuffer = m_AircraftNavigationLanes[entity];
		if (navigationEnd)
		{
			if (dynamicBuffer.Length != 0)
			{
				AircraftNavigationLane aircraftNavigationLane = dynamicBuffer[dynamicBuffer.Length - 1];
				lane = aircraftNavigationLane.m_Lane;
				curvePos = aircraftNavigationLane.m_CurvePosition.y;
				flags = aircraftNavigationLane.m_Flags;
			}
			return;
		}
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			AircraftNavigationLane aircraftNavigationLane2 = dynamicBuffer[i];
			if ((aircraftNavigationLane2.m_Flags & AircraftLaneFlags.Reserved) == 0)
			{
				break;
			}
			lane = aircraftNavigationLane2.m_Lane;
			curvePos = aircraftNavigationLane2.m_CurvePosition.y;
			flags = aircraftNavigationLane2.m_Flags;
		}
	}

	private bool GetEdge(Entity lane, out Entity edge)
	{
		if (m_Owner.HasComponent(lane))
		{
			Owner owner = m_Owner[lane];
			if (m_SubLane.HasBuffer(owner.m_Owner))
			{
				edge = owner.m_Owner;
				return true;
			}
		}
		edge = Entity.Null;
		return false;
	}

	private int AddSubLaneTargets(ref Unity.Mathematics.Random random, Entity target, Entity entity, float cost, bool randomCurvePos, bool allowAccessRestriction, EdgeFlags flags)
	{
		int num = 0;
		Entity entity2 = entity;
		while (m_Owner.HasComponent(entity2))
		{
			entity2 = m_Owner[entity2].m_Owner;
		}
		DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLane[entity];
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			Entity subLane = dynamicBuffer[i].m_SubLane;
			float curvePos = 0.5f;
			if (randomCurvePos)
			{
				curvePos = random.NextFloat();
			}
			num += AddLaneTarget(ref random, target, entity2, subLane, curvePos, cost, flags, allowAccessRestriction);
		}
		return num;
	}

	public int AddAreaTargets(ref Unity.Mathematics.Random random, Entity target, Entity entity, Entity subItem, DynamicBuffer<Game.Areas.SubArea> subAreas, float cost, bool addDistanceCost, EdgeFlags flags)
	{
		if (!m_Transform.HasComponent(subItem))
		{
			int num = 0;
			if (m_SubLane.HasBuffer(entity))
			{
				num += AddSubLaneTargets(ref random, target, entity, cost, m_SetupQueueTarget.m_RandomCost != 0f, allowAccessRestriction: true, flags);
			}
			if (subAreas.IsCreated)
			{
				for (int i = 0; i < subAreas.Length; i++)
				{
					Game.Areas.SubArea subArea = subAreas[i];
					if (m_SubLane.HasBuffer(subArea.m_Area))
					{
						num += AddSubLaneTargets(ref random, target, subArea.m_Area, cost, m_SetupQueueTarget.m_RandomCost != 0f, allowAccessRestriction: true, flags);
					}
				}
			}
			return num;
		}
		Game.Objects.Transform transform = m_Transform[subItem];
		int num2 = 0;
		if (subAreas.IsCreated)
		{
			num2 = subAreas.Length;
		}
		float num3 = float.MaxValue;
		Entity entity2 = Entity.Null;
		float delta = 0f;
		for (int j = -1; j < num2; j++)
		{
			if (j >= 0)
			{
				entity = subAreas[j].m_Area;
			}
			if (!m_SubLane.TryGetBuffer(entity, out var bufferData) || bufferData.Length == 0)
			{
				continue;
			}
			DynamicBuffer<Game.Areas.Node> nodes = m_AreaNode[entity];
			DynamicBuffer<Triangle> dynamicBuffer = m_AreaTriangle[entity];
			float num4 = num3;
			float3 position = transform.m_Position;
			Triangle3 triangle = default(Triangle3);
			for (int k = 0; k < dynamicBuffer.Length; k++)
			{
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, dynamicBuffer[k]);
				float2 t;
				float num5 = MathUtils.Distance(triangle2, transform.m_Position, out t);
				if (num5 < num4)
				{
					num4 = num5;
					position = MathUtils.Position(triangle2, t);
					triangle = triangle2;
				}
			}
			if (num4 == num3)
			{
				continue;
			}
			float num6 = float.MaxValue;
			for (int l = 0; l < bufferData.Length; l++)
			{
				Entity subLane = bufferData[l].m_SubLane;
				if (!m_ConnectionLane.HasComponent(subLane))
				{
					continue;
				}
				Game.Net.ConnectionLane connectionLane = m_ConnectionLane[subLane];
				if (((m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) == 0 || (connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) == 0) && ((m_SetupQueueTarget.m_Methods & PathMethod.Offroad) == 0 || (connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0 || (m_SetupQueueTarget.m_RoadTypes & connectionLane.m_RoadTypes) == 0))
				{
					continue;
				}
				Curve curve = m_Curve[subLane];
				if (MathUtils.Intersect(triangle.xz, curve.m_Bezier.a.xz, out var t2) || MathUtils.Intersect(triangle.xz, curve.m_Bezier.d.xz, out t2))
				{
					float t3;
					float num7 = MathUtils.Distance(curve.m_Bezier, position, out t3);
					if (num7 < num6)
					{
						num3 = num4;
						entity2 = subLane;
						delta = t3;
						num6 = num7;
					}
				}
			}
		}
		if (entity2 != Entity.Null)
		{
			cost += math.select(0f, num3, addDistanceCost);
			AddTarget(ref random, target, entity2, delta, cost, flags);
			return 1;
		}
		return 0;
	}

	private int AddLaneTarget(ref Unity.Mathematics.Random random, Entity target, Entity accessRequirement, Entity lane, float curvePos, float cost, EdgeFlags flags, bool allowAccessRestriction)
	{
		if ((m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0 && m_PedestrianLane.HasComponent(lane))
		{
			Game.Net.PedestrianLane pedestrianLane = m_PedestrianLane[lane];
			if (CheckAccessRestriction(allowAccessRestriction, pedestrianLane) || pedestrianLane.m_AccessRestriction == accessRequirement)
			{
				AddTarget(ref random, target, lane, curvePos, cost, flags);
				return 1;
			}
		}
		if ((m_SetupQueueTarget.m_Methods & PathMethod.Road) != 0 && m_CarLane.HasComponent(lane) && !m_SlaveLane.HasComponent(lane))
		{
			Game.Net.CarLane carLane = m_CarLane[lane];
			if (CheckAccessRestriction(allowAccessRestriction, carLane) || carLane.m_AccessRestriction == accessRequirement)
			{
				PrefabRef prefabRef = m_PrefabRef[lane];
				CarLaneData carLaneData = m_CarLaneData[prefabRef.m_Prefab];
				if (VehicleUtils.CanUseLane(m_SetupQueueTarget.m_Methods, m_SetupQueueTarget.m_RoadTypes, carLaneData))
				{
					AddTarget(ref random, target, lane, curvePos, cost, flags);
					return 1;
				}
			}
		}
		if ((m_SetupQueueTarget.m_Methods & (PathMethod.Parking | PathMethod.Boarding | PathMethod.SpecialParking | PathMethod.BicycleParking)) != 0 && m_ParkingLane.TryGetComponent(lane, out var componentData) && (CheckAccessRestriction(allowAccessRestriction, componentData) || componentData.m_AccessRestriction == accessRequirement))
		{
			PrefabRef prefabRef2 = m_PrefabRef[lane];
			ParkingLaneData parkingLaneData = m_ParkingLaneData[prefabRef2.m_Prefab];
			PathMethod pathMethod = ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
			if ((componentData.m_Flags & ParkingLaneFlags.SpecialVehicles) != 0)
			{
				pathMethod |= PathMethod.Boarding | PathMethod.SpecialParking;
			}
			else
			{
				if ((parkingLaneData.m_RoadTypes & ~RoadTypes.Bicycle) != RoadTypes.None)
				{
					pathMethod |= PathMethod.Parking | PathMethod.Boarding;
				}
				if ((parkingLaneData.m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
				{
					pathMethod |= PathMethod.BicycleParking;
				}
			}
			if ((m_SetupQueueTarget.m_RoadTypes & parkingLaneData.m_RoadTypes) != RoadTypes.None && (m_SetupQueueTarget.m_Methods & pathMethod) != 0)
			{
				AddTarget(ref random, target, lane, curvePos, cost, flags);
				return 1;
			}
		}
		if ((m_SetupQueueTarget.m_Methods & PathMethod.Track) != 0)
		{
			PrefabRef prefabRef3 = m_PrefabRef[lane];
			if (m_TrackLaneData.HasComponent(prefabRef3.m_Prefab))
			{
				TrackLaneData trackLaneData = m_TrackLaneData[prefabRef3.m_Prefab];
				if ((m_SetupQueueTarget.m_TrackTypes & trackLaneData.m_TrackTypes) != TrackTypes.None)
				{
					AddTarget(ref random, target, lane, curvePos, cost, flags);
					return 1;
				}
			}
		}
		if (m_ConnectionLane.HasComponent(lane))
		{
			Game.Net.ConnectionLane connectionLane = m_ConnectionLane[lane];
			if ((CheckAccessRestriction(allowAccessRestriction, connectionLane) || connectionLane.m_AccessRestriction == accessRequirement) && (connectionLane.m_Flags & ConnectionLaneFlags.Inside) == 0 && (((m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0 && (connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) != 0) || ((m_SetupQueueTarget.m_Methods & PathMethod.Road) != 0 && (connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 && (m_SetupQueueTarget.m_RoadTypes & connectionLane.m_RoadTypes) != RoadTypes.None) || ((m_SetupQueueTarget.m_Methods & PathMethod.Track) != 0 && (connectionLane.m_Flags & ConnectionLaneFlags.Track) != 0 && (m_SetupQueueTarget.m_TrackTypes & connectionLane.m_TrackTypes) != TrackTypes.None) || ((m_SetupQueueTarget.m_Methods & PathMethod.CargoLoading) != 0 && (connectionLane.m_Flags & ConnectionLaneFlags.AllowCargo) != 0)))
			{
				curvePos = math.select(curvePos, 1f, (connectionLane.m_Flags & ConnectionLaneFlags.Start) != 0);
				AddTarget(ref random, target, lane, curvePos, cost, flags);
				return 1;
			}
		}
		return 0;
	}

	private int AddTrainTargets(ref Unity.Mathematics.Random random, Entity target, Entity entity, float cost, EdgeFlags flags)
	{
		int num = 0;
		if (m_VehicleLayout.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
		{
			Entity vehicle = bufferData[0].m_Vehicle;
			Entity vehicle2 = bufferData[bufferData.Length - 1].m_Vehicle;
			ParkedTrain componentData2;
			if (m_TrainCurrentLane.TryGetComponent(vehicle, out var componentData))
			{
				num += AddTrainTarget(ref random, target, cost, flags, vehicle, componentData.m_Front.m_Lane, componentData.m_Front.m_CurvePosition.w, trainForward: true);
			}
			else if (m_ParkedTrain.TryGetComponent(vehicle, out componentData2))
			{
				num += AddTrainTarget(ref random, target, cost, flags, vehicle, componentData2.m_FrontLane, componentData2.m_CurvePosition.x, trainForward: true);
			}
			ParkedTrain componentData4;
			if (m_TrainCurrentLane.TryGetComponent(vehicle2, out var componentData3))
			{
				num += AddTrainTarget(ref random, target, cost, flags, vehicle2, componentData3.m_Rear.m_Lane, componentData3.m_Rear.m_CurvePosition.y, trainForward: false);
			}
			else if (m_ParkedTrain.TryGetComponent(vehicle2, out componentData4))
			{
				num += AddTrainTarget(ref random, target, cost, flags, vehicle2, componentData4.m_RearLane, componentData4.m_CurvePosition.y, trainForward: false);
			}
			if (num != 0)
			{
				return num;
			}
		}
		ParkedTrain componentData6;
		if (m_TrainCurrentLane.TryGetComponent(entity, out var componentData5))
		{
			num += AddTrainTarget(ref random, target, cost, flags, entity, componentData5.m_Front.m_Lane, componentData5.m_Front.m_CurvePosition.w, trainForward: true);
			num += AddTrainTarget(ref random, target, cost, flags, entity, componentData5.m_Rear.m_Lane, componentData5.m_Rear.m_CurvePosition.y, trainForward: false);
		}
		else if (m_ParkedTrain.TryGetComponent(entity, out componentData6))
		{
			num += AddTrainTarget(ref random, target, cost, flags, entity, componentData6.m_FrontLane, componentData6.m_CurvePosition.x, trainForward: true);
			num += AddTrainTarget(ref random, target, cost, flags, entity, componentData6.m_RearLane, componentData6.m_CurvePosition.y, trainForward: false);
		}
		return num;
	}

	private int AddTrainTarget(ref Unity.Mathematics.Random random, Entity target, float cost, EdgeFlags flags, Entity carriage, Entity lane, float curvePosition, bool trainForward)
	{
		if (m_Curve.TryGetComponent(lane, out var componentData))
		{
			Train train = m_Train[carriage];
			bool flag = math.dot(math.forward(m_Transform[carriage].m_Rotation), MathUtils.Tangent(componentData.m_Bezier, curvePosition)) >= 0f;
			flag ^= (train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0 == trainForward;
			flags = (EdgeFlags)((uint)flags & (uint)(ushort)(~((!flag) ? 1 : 2)));
			AddTarget(ref random, target, lane, curvePosition, cost, flags);
			return 1;
		}
		return 0;
	}

	public int AddLaneConnectionTargets(ref Unity.Mathematics.Random random, Entity target, float cost, EdgeFlags flags, LaneConnection laneConnection, float3 comparePosition, float maxDistance, bool allowLaneGroupSwitch, bool allowAccessRestriction)
	{
		int num = 0;
		if ((m_SetupQueueTarget.m_Methods & PathMethod.Pedestrian) != 0 && laneConnection.m_EndLane != Entity.Null)
		{
			Game.Net.ConnectionLane componentData2;
			if (m_PedestrianLane.TryGetComponent(laneConnection.m_EndLane, out var componentData))
			{
				if (CheckAccessRestriction(allowAccessRestriction, componentData))
				{
					goto IL_007e;
				}
			}
			else if (m_ConnectionLane.TryGetComponent(laneConnection.m_EndLane, out componentData2) && (componentData2.m_Flags & ConnectionLaneFlags.Pedestrian) != 0 && CheckAccessRestriction(allowAccessRestriction, componentData2))
			{
				goto IL_007e;
			}
		}
		goto IL_00b8;
		IL_01bf:
		MathUtils.Distance(m_Curve[laneConnection.m_StartLane].m_Bezier, comparePosition, out var t);
		num += AddCarLaneTargets(ref random, target, laneConnection.m_StartLane, comparePosition, maxDistance, t, cost, flags, allowLaneGroupSwitch, allowBlocked: false, allowAccessRestriction);
		goto IL_0200;
		IL_007e:
		float t2;
		float distance = MathUtils.Distance(m_Curve[laneConnection.m_EndLane].m_Bezier, comparePosition, out t2);
		num += AddPedestrianLaneTargets(ref random, target, laneConnection.m_EndLane, t2, cost, distance, flags, allowAccessRestriction);
		goto IL_00b8;
		IL_00b8:
		if ((m_SetupQueueTarget.m_Methods & PathMethod.Road) != 0 && laneConnection.m_StartLane != Entity.Null)
		{
			if (m_PrefabRef.TryGetComponent(laneConnection.m_StartLane, out var componentData3) && m_CarLaneData.TryGetComponent(componentData3.m_Prefab, out var componentData4) && !m_MasterLane.HasComponent(laneConnection.m_StartLane))
			{
				Game.Net.CarLane carLane = m_CarLane[laneConnection.m_StartLane];
				if (VehicleUtils.CanUseLane(m_SetupQueueTarget.m_Methods, m_SetupQueueTarget.m_RoadTypes, componentData4) && CheckAccessRestriction(allowAccessRestriction, carLane))
				{
					goto IL_01bf;
				}
			}
			else if (m_ConnectionLane.HasComponent(laneConnection.m_StartLane))
			{
				Game.Net.ConnectionLane connectionLane = m_ConnectionLane[laneConnection.m_StartLane];
				if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 && (connectionLane.m_RoadTypes & m_SetupQueueTarget.m_RoadTypes) != RoadTypes.None && CheckAccessRestriction(allowAccessRestriction, connectionLane))
				{
					goto IL_01bf;
				}
			}
		}
		goto IL_0200;
		IL_0200:
		return num;
	}

	public int AddEdgeTargets(ref Unity.Mathematics.Random random, Entity target, float cost, EdgeFlags flags, Entity edge, float3 comparePosition, float maxDistance, bool allowLaneGroupSwitch, bool allowAccessRestriction)
	{
		DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLane[edge];
		float num = float.MaxValue;
		float curvePos = 0f;
		Entity entity = Entity.Null;
		float num2 = float.MaxValue;
		float curvePos2 = 0f;
		Entity entity2 = Entity.Null;
		float num3 = float.MaxValue;
		float delta = 0f;
		Entity entity3 = Entity.Null;
		float num4 = float.MaxValue;
		float delta2 = 0f;
		Entity entity4 = Entity.Null;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			Game.Net.SubLane subLane = dynamicBuffer[i];
			PathMethod pathMethod = m_SetupQueueTarget.m_Methods & subLane.m_PathMethods;
			if (pathMethod == ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking))
			{
				continue;
			}
			if ((pathMethod & PathMethod.Pedestrian) != 0)
			{
				if (m_PedestrianLane.HasComponent(subLane.m_SubLane))
				{
					Game.Net.PedestrianLane pedestrianLane = m_PedestrianLane[subLane.m_SubLane];
					if (CheckAccessRestriction(allowAccessRestriction, pedestrianLane))
					{
						goto IL_0116;
					}
				}
				else if (m_ConnectionLane.HasComponent(subLane.m_SubLane))
				{
					Game.Net.ConnectionLane connectionLane = m_ConnectionLane[subLane.m_SubLane];
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) != 0 && CheckAccessRestriction(allowAccessRestriction, connectionLane))
					{
						goto IL_0116;
					}
				}
			}
			goto IL_016a;
			IL_0116:
			Curve curve = m_Curve[subLane.m_SubLane];
			if (MathUtils.Distance(MathUtils.Bounds(curve.m_Bezier), comparePosition) < num)
			{
				float t;
				float num5 = MathUtils.Distance(curve.m_Bezier, comparePosition, out t);
				if (num5 < num)
				{
					num = num5;
					curvePos = t;
					entity = subLane.m_SubLane;
					continue;
				}
			}
			goto IL_016a;
			IL_037c:
			Curve curve2 = m_Curve[subLane.m_SubLane];
			if (MathUtils.Distance(MathUtils.Bounds(curve2.m_Bezier), comparePosition) < num3)
			{
				float t2;
				float num6 = MathUtils.Distance(curve2.m_Bezier, comparePosition, out t2);
				if (num6 < num3)
				{
					num3 = num6;
					delta = t2;
					entity3 = subLane.m_SubLane;
					continue;
				}
			}
			goto IL_03d5;
			IL_02bf:
			if ((pathMethod & (PathMethod.Parking | PathMethod.Boarding | PathMethod.SpecialParking | PathMethod.BicycleParking)) != 0)
			{
				PrefabRef prefabRef = m_PrefabRef[subLane.m_SubLane];
				Game.Net.ConnectionLane componentData2;
				if (m_ParkingLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					Game.Net.ParkingLane parkingLane = m_ParkingLane[subLane.m_SubLane];
					if ((m_SetupQueueTarget.m_RoadTypes & componentData.m_RoadTypes) != RoadTypes.None && CheckAccessRestriction(allowAccessRestriction, parkingLane))
					{
						goto IL_037c;
					}
				}
				else if (m_ConnectionLane.TryGetComponent(subLane.m_SubLane, out componentData2) && (componentData2.m_Flags & ConnectionLaneFlags.Parking) != 0 && (componentData2.m_RoadTypes & m_SetupQueueTarget.m_RoadTypes) != RoadTypes.None && CheckAccessRestriction(allowAccessRestriction, componentData2))
				{
					goto IL_037c;
				}
			}
			goto IL_03d5;
			IL_016a:
			if ((pathMethod & (PathMethod.Road | PathMethod.Bicycle)) != 0)
			{
				PrefabRef prefabRef2 = m_PrefabRef[subLane.m_SubLane];
				if (m_CarLaneData.HasComponent(prefabRef2.m_Prefab) && !m_MasterLane.HasComponent(subLane.m_SubLane))
				{
					Game.Net.CarLane carLane = m_CarLane[subLane.m_SubLane];
					CarLaneData carLaneData = m_CarLaneData[prefabRef2.m_Prefab];
					if (VehicleUtils.CanUseLane(m_SetupQueueTarget.m_Methods, m_SetupQueueTarget.m_RoadTypes, carLaneData) && CheckAccessRestriction(allowAccessRestriction, carLane))
					{
						goto IL_0266;
					}
				}
				else if (m_ConnectionLane.HasComponent(subLane.m_SubLane))
				{
					Game.Net.ConnectionLane connectionLane2 = m_ConnectionLane[subLane.m_SubLane];
					if ((connectionLane2.m_Flags & ConnectionLaneFlags.Road) != 0 && (connectionLane2.m_RoadTypes & m_SetupQueueTarget.m_RoadTypes) != RoadTypes.None && CheckAccessRestriction(allowAccessRestriction, connectionLane2))
					{
						goto IL_0266;
					}
				}
			}
			goto IL_02bf;
			IL_03d5:
			if ((pathMethod & PathMethod.Track) == 0)
			{
				continue;
			}
			PrefabRef prefabRef3 = m_PrefabRef[subLane.m_SubLane];
			if (m_TrackLaneData.HasComponent(prefabRef3.m_Prefab))
			{
				TrackLaneData trackLaneData = m_TrackLaneData[prefabRef3.m_Prefab];
				if ((m_SetupQueueTarget.m_TrackTypes & trackLaneData.m_TrackTypes) == 0)
				{
					continue;
				}
			}
			else
			{
				if (!m_ConnectionLane.HasComponent(subLane.m_SubLane))
				{
					continue;
				}
				Game.Net.ConnectionLane connectionLane3 = m_ConnectionLane[subLane.m_SubLane];
				if ((connectionLane3.m_Flags & ConnectionLaneFlags.Track) == 0 || (connectionLane3.m_TrackTypes & m_SetupQueueTarget.m_TrackTypes) == 0)
				{
					continue;
				}
			}
			Curve curve3 = m_Curve[subLane.m_SubLane];
			if (MathUtils.Distance(MathUtils.Bounds(curve3.m_Bezier), comparePosition) < num4)
			{
				float t3;
				float num7 = MathUtils.Distance(curve3.m_Bezier, comparePosition, out t3);
				if (num7 < num4)
				{
					num4 = num7;
					delta2 = t3;
					entity4 = subLane.m_SubLane;
				}
			}
			continue;
			IL_0266:
			Curve curve4 = m_Curve[subLane.m_SubLane];
			if (MathUtils.Distance(MathUtils.Bounds(curve4.m_Bezier), comparePosition) < num2)
			{
				float t4;
				float num8 = MathUtils.Distance(curve4.m_Bezier, comparePosition, out t4);
				if (num8 < num2)
				{
					num2 = num8;
					curvePos2 = t4;
					entity2 = subLane.m_SubLane;
					continue;
				}
			}
			goto IL_02bf;
		}
		int num9 = 0;
		if (entity != Entity.Null)
		{
			num9 += AddPedestrianLaneTargets(ref random, target, entity, curvePos, cost, num, flags, allowAccessRestriction);
		}
		if (entity2 != Entity.Null)
		{
			num9 += AddCarLaneTargets(ref random, target, entity2, comparePosition, maxDistance, curvePos2, cost, flags, allowLaneGroupSwitch, allowBlocked: false, allowAccessRestriction);
		}
		if (entity3 != Entity.Null)
		{
			AddTarget(ref random, target, entity3, delta, cost, flags);
			num9++;
		}
		if (entity4 != Entity.Null)
		{
			AddTarget(ref random, target, entity4, delta2, cost, flags);
			num9++;
		}
		return num9;
	}

	private int AddPedestrianLaneTargets(ref Unity.Mathematics.Random random, Entity target, Entity lane, float curvePos, float cost, float distance, EdgeFlags flags, bool allowAccessRestriction)
	{
		Game.Net.ConnectionLane componentData2;
		if (m_PedestrianLane.TryGetComponent(lane, out var componentData))
		{
			if (!CheckAccessRestriction(allowAccessRestriction, componentData))
			{
				return 0;
			}
		}
		else if (m_ConnectionLane.TryGetComponent(lane, out componentData2) && !CheckAccessRestriction(allowAccessRestriction, componentData2))
		{
			return 0;
		}
		float cost2 = cost + CalculatePedestrianTargetCost(ref random, distance);
		AddTarget(ref random, target, lane, curvePos, cost2, flags);
		return 1;
	}

	private int AddParkingLaneTargets(ref Unity.Mathematics.Random random, Entity target, Entity lane, float curvePos, float cost, EdgeFlags flags, bool allowAccessRestriction)
	{
		Game.Net.ConnectionLane componentData2;
		if (m_ParkingLane.TryGetComponent(lane, out var componentData))
		{
			if (!CheckAccessRestriction(allowAccessRestriction, componentData))
			{
				return 0;
			}
		}
		else if (m_ConnectionLane.TryGetComponent(lane, out componentData2) && !CheckAccessRestriction(allowAccessRestriction, componentData2))
		{
			return 0;
		}
		AddTarget(ref random, target, lane, curvePos, cost, flags);
		return 1;
	}

	private int AddCarLaneTargets(ref Unity.Mathematics.Random random, Entity target, Entity lane, float3 comparePosition, float maxDistance, float curvePos, float cost, EdgeFlags flags, bool allowLaneGroupSwitch, bool allowBlocked, bool allowAccessRestriction)
	{
		if (!m_CarLane.TryGetComponent(lane, out var componentData))
		{
			Game.Net.PedestrianLane componentData3;
			Game.Net.ParkingLane componentData4;
			if (m_ConnectionLane.TryGetComponent(lane, out var componentData2))
			{
				if (!CheckAccessRestriction(allowAccessRestriction, componentData2))
				{
					return 0;
				}
				if ((componentData2.m_Flags & ConnectionLaneFlags.Pedestrian) != 0 && (componentData2.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) != 0)
				{
					flags |= EdgeFlags.Secondary;
				}
			}
			else if (m_PedestrianLane.TryGetComponent(lane, out componentData3))
			{
				if (!CheckAccessRestriction(allowAccessRestriction, componentData3))
				{
					return 0;
				}
				flags |= EdgeFlags.Secondary;
			}
			else if (m_ParkingLane.TryGetComponent(lane, out componentData4))
			{
				if (!CheckAccessRestriction(allowAccessRestriction, componentData4))
				{
					return 0;
				}
				if ((componentData4.m_Flags & ParkingLaneFlags.SecondaryStart) != 0)
				{
					flags |= EdgeFlags.Secondary;
				}
				else if ((componentData4.m_Flags & ParkingLaneFlags.AdditionalStart) != 0)
				{
					AddTarget(ref random, target, lane, curvePos, cost, flags);
					flags |= EdgeFlags.Secondary;
				}
			}
			AddTarget(ref random, target, lane, curvePos, cost, flags);
			return 1;
		}
		Owner owner = m_Owner[lane];
		SlaveLane slaveLane = default(SlaveLane);
		if (!CheckAccessRestriction(allowAccessRestriction, componentData))
		{
			return 0;
		}
		PrefabRef prefabRef = m_PrefabRef[lane];
		NetLaneData netLaneData = m_NetLaneData[prefabRef.m_Prefab];
		PathfindCarData pathfindCarData = m_CarPathfindData[netLaneData.m_PathfindPrefab];
		if ((componentData.m_Flags & (Game.Net.CarLaneFlags.SecondaryStart | Game.Net.CarLaneFlags.SecondaryEnd)) != 0 && m_CarLaneData.TryGetComponent(prefabRef, out var componentData5) && componentData5.m_RoadTypes == RoadTypes.Bicycle)
		{
			flags |= EdgeFlags.Secondary;
		}
		float num = 0f;
		if (m_NodeLane.TryGetComponent(lane, out var componentData6))
		{
			num = (netLaneData.m_Width + math.lerp(componentData6.m_WidthOffset.x, componentData6.m_WidthOffset.y, curvePos)) * 0.5f;
		}
		bool flag = false;
		if (m_SlaveLane.HasComponent(lane))
		{
			slaveLane = m_SlaveLane[lane];
			num *= (float)(slaveLane.m_MaxIndex - slaveLane.m_MinIndex + 1);
			flag = true;
		}
		DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLane[owner.m_Owner];
		int trueValue = slaveLane.m_MaxIndex - slaveLane.m_MinIndex + 1;
		int num2 = 0;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			Game.Net.SubLane subLane = dynamicBuffer[i];
			if ((subLane.m_PathMethods & m_SetupQueueTarget.m_Methods) == 0 || !m_CarLane.HasComponent(subLane.m_SubLane) || m_SlaveLane.HasComponent(subLane.m_SubLane))
			{
				continue;
			}
			Game.Net.CarLane carLaneData = m_CarLane[subLane.m_SubLane];
			if (carLaneData.m_CarriagewayGroup != componentData.m_CarriagewayGroup || carLaneData.m_AccessRestriction != componentData.m_AccessRestriction)
			{
				continue;
			}
			bool flag2;
			int num3;
			if (m_MasterLane.HasComponent(subLane.m_SubLane))
			{
				MasterLane masterLane = m_MasterLane[subLane.m_SubLane];
				flag2 = !flag || masterLane.m_Group != slaveLane.m_Group;
				num3 = masterLane.m_MaxIndex - masterLane.m_MinIndex + 1;
			}
			else
			{
				flag2 = subLane.m_SubLane != lane;
				num3 = 1;
			}
			float t = math.select(curvePos, 1f - curvePos, ((componentData.m_Flags ^ carLaneData.m_Flags) & Game.Net.CarLaneFlags.Invert) != 0);
			if (flag2)
			{
				if ((componentData.m_Flags & (Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout)) == Game.Net.CarLaneFlags.Roundabout)
				{
					continue;
				}
				if (num != 0f)
				{
					if (!m_NodeLane.TryGetComponent(subLane.m_SubLane, out var componentData7))
					{
						continue;
					}
					Curve curve = m_Curve[lane];
					Curve curve2 = m_Curve[subLane.m_SubLane];
					float3 position = MathUtils.Position(curve.m_Bezier, curvePos);
					float num4 = MathUtils.Distance(curve2.m_Bezier, position, out t);
					PrefabRef prefabRef2 = m_PrefabRef[subLane.m_SubLane];
					float num5 = (m_NetLaneData[prefabRef2.m_Prefab].m_Width + math.lerp(componentData7.m_WidthOffset.x, componentData7.m_WidthOffset.y, t)) * 0.5f * (float)num3;
					if (num4 > num + num5 + 3f)
					{
						continue;
					}
				}
			}
			if (carLaneData.m_BlockageEnd >= carLaneData.m_BlockageStart && (m_PathfindParameters.m_IgnoredRules & RuleFlags.HasBlockage) == 0)
			{
				Bounds1 blockageBounds = carLaneData.blockageBounds;
				if (maxDistance != 0f && (carLaneData.m_Flags & Game.Net.CarLaneFlags.Twoway) == 0 && carLaneData.m_BlockageStart > 0 && math.distance(MathUtils.Position(m_Curve[subLane.m_SubLane].m_Bezier, blockageBounds.min), comparePosition) <= maxDistance)
				{
					t = math.max(0f, blockageBounds.min - 0.01f);
				}
				else if (MathUtils.Intersect(blockageBounds, t))
				{
					if (((t - blockageBounds.min < blockageBounds.max - t && (carLaneData.m_Flags & Game.Net.CarLaneFlags.Twoway) != 0) || carLaneData.m_BlockageEnd == byte.MaxValue) && carLaneData.m_BlockageStart > 0)
					{
						t = math.max(0f, blockageBounds.min - 0.01f);
					}
					else
					{
						if (carLaneData.m_BlockageEnd >= byte.MaxValue || (!allowBlocked && blockageBounds.max + 0.01f > t))
						{
							continue;
						}
						t = math.min(1f, blockageBounds.max + 0.01f);
					}
				}
			}
			float distance = math.distance(comparePosition, MathUtils.Position(m_Curve[subLane.m_SubLane].m_Bezier, curvePos));
			float cost2 = cost + CalculateCarTargetCost(ref random, pathfindCarData, carLaneData, distance, math.select(0, trueValue, flag2), !allowLaneGroupSwitch && flag2);
			AddTarget(ref random, target, subLane.m_SubLane, t, cost2, flags);
			num2++;
		}
		return num2;
	}

	private float CalculatePedestrianTargetCost(ref Unity.Mathematics.Random random, float distance)
	{
		PathSpecification pathSpecification = new PathSpecification
		{
			m_Flags = (EdgeFlags.Forward | EdgeFlags.Backward),
			m_Methods = PathMethod.Pedestrian,
			m_Length = distance,
			m_MaxSpeed = 5.555556f,
			m_Density = 0f,
			m_AccessRequirement = -1
		};
		return PathUtils.CalculateCost(ref random, in pathSpecification, in m_PathfindParameters);
	}

	private float CalculateCarTargetCost(ref Unity.Mathematics.Random random, PathfindCarData pathfindCarData, Game.Net.CarLane carLaneData, float distance, int laneCrossCount, bool unsafeUTurn)
	{
		PathSpecification pathSpecification = new PathSpecification
		{
			m_Flags = (EdgeFlags.Forward | EdgeFlags.Backward),
			m_Methods = PathMethod.Road,
			m_Length = distance,
			m_MaxSpeed = carLaneData.m_SpeedLimit,
			m_Density = 0f,
			m_AccessRequirement = -1
		};
		PathUtils.TryAddCosts(ref pathSpecification.m_Costs, pathfindCarData.m_LaneCrossCost, laneCrossCount);
		PathUtils.TryAddCosts(ref pathSpecification.m_Costs, pathfindCarData.m_UnsafeUTurnCost, unsafeUTurn);
		return PathUtils.CalculateCost(ref random, in pathSpecification, in m_PathfindParameters);
	}
}
