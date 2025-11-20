using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Vehicles;

public static class VehicleUtils
{
	public const float MAX_VEHICLE_SPEED = 277.77777f;

	public const float MAX_CAR_SPEED = 111.111115f;

	public const float MAX_TRAIN_SPEED = 138.88889f;

	public const float MAX_WATERCRAFT_SPEED = 55.555557f;

	public const float MAX_HELICOPTER_SPEED = 83.333336f;

	public const float MAX_AIRPLANE_SPEED = 277.77777f;

	public const float MAX_CAR_LENGTH = 20f;

	public const float PARALLEL_PARKING_OFFSET = 1f;

	public const float CAR_CRAWL_SPEED = 3f;

	public const float CAR_AREA_SPEED = 11.111112f;

	public const float VEHICLE_PEDESTRIAN_SPEED = 5.555556f;

	public const float MIN_HIGHWAY_SPEED = 22.222223f;

	public const float MAX_WATERCRAFT_LENGTH = 150f;

	public const float WATERCRAFT_AREA_SPEED = 11.111112f;

	public const float MAX_FIRE_ENGINE_EXTINGUISH_DISTANCE = 30f;

	public const float MAX_POLICE_ACCIDENT_TARGET_DISTANCE = 30f;

	public const float MAX_MAINTENANCE_TARGET_DISTANCE = 30f;

	public const uint MAINTENANCE_DESTROYED_CLEAR_AMOUNT = 500u;

	public const float MAX_TRAIN_LENGTH = 200f;

	public const float TRAIN_CRAWL_SPEED = 3f;

	public const float MAX_TRAIN_CARRIAGE_LENGTH = 20f;

	public const float MAX_TRAM_CARRIAGE_LENGTH = 16f;

	public const float MAX_SUBWAY_LENGTH = 200f;

	public const float MAX_SUBWAY_CARRIAGE_LENGTH = 18f;

	public const int CAR_NAVIGATION_LANE_CAPACITY = 8;

	public const int CAR_PARALLEL_LANE_CAPACITY = 8;

	public const int WATERCRAFT_NAVIGATION_LANE_CAPACITY = 8;

	public const int WATERCRAFT_PARALLEL_LANE_CAPACITY = 8;

	public const int AIRCRAFT_NAVIGATION_LANE_CAPACITY = 8;

	public const float MIN_HELICOPTER_NAVIGATION_DISTANCE = 750f;

	public const float MIN_AIRPLANE_NAVIGATION_DISTANCE = 1500f;

	public const float AIRPLANE_FLY_HEIGHT = 1000f;

	public const float HELICOPTER_FLY_HEIGHT = 100f;

	public const float ROCKET_FLY_HEIGHT = 10000f;

	public const uint BOTTLENECK_LIMIT = 50u;

	public const uint STUCK_MAX_COUNT = 100u;

	public const int STUCK_MAX_SPEED = 6;

	public const float TEMP_WAIT_TIME = 5f;

	public const float DELIVERY_PATHFIND_RANDOM_COST = 30f;

	public const float SERVICE_PATHFIND_RANDOM_COST = 30f;

	public const int PRIORITY_OFFSET = 2;

	public const int NORMAL_CAR_PRIORITY = 100;

	public const int TRACK_RESERVE_PRIORITY = 98;

	public const int REQUEST_SPACE_PRIORITY = 96;

	public const int EMERGENCY_YIELD_PRIORITY = 102;

	public const int NORMAL_TRAIN_PRIORITY = 104;

	public const int EMERGENCY_FLEE_PRIORITY = 106;

	public const int EMERGENCY_CAR_PRIORITY = 108;

	public const int PRIMARY_TRAIN_PRIORITY = 110;

	public const int SMALL_WATERCRAFT_PRIORITY = 100;

	public const int MEDIUM_WATERCRAFT_PRIORITY = 102;

	public const int LARGE_WATERCRAFT_PRIORITY = 104;

	public const int NORMAL_AIRCRAFT_PRIORITY = 104;

	public static bool PathfindFailed(PathOwner pathOwner)
	{
		return (pathOwner.m_State & (PathFlags.Failed | PathFlags.Stuck)) != 0;
	}

	public static bool PathEndReached(CarCurrentLane currentLane)
	{
		return (currentLane.m_LaneFlags & (CarLaneFlags.EndOfPath | CarLaneFlags.EndReached | CarLaneFlags.ParkingSpace | CarLaneFlags.Waypoint)) == (CarLaneFlags.EndOfPath | CarLaneFlags.EndReached);
	}

	public static bool PathEndReached(TrainCurrentLane currentLane)
	{
		return (currentLane.m_Front.m_LaneFlags & (TrainLaneFlags.EndOfPath | TrainLaneFlags.EndReached)) == (TrainLaneFlags.EndOfPath | TrainLaneFlags.EndReached);
	}

	public static bool ReturnEndReached(TrainCurrentLane currentLane)
	{
		return (currentLane.m_Front.m_LaneFlags & (TrainLaneFlags.EndReached | TrainLaneFlags.Return)) == (TrainLaneFlags.EndReached | TrainLaneFlags.Return);
	}

	public static bool PathEndReached(WatercraftCurrentLane currentLane)
	{
		return (currentLane.m_LaneFlags & (WatercraftLaneFlags.EndOfPath | WatercraftLaneFlags.EndReached)) == (WatercraftLaneFlags.EndOfPath | WatercraftLaneFlags.EndReached);
	}

	public static bool PathEndReached(AircraftCurrentLane currentLane)
	{
		return (currentLane.m_LaneFlags & (AircraftLaneFlags.EndOfPath | AircraftLaneFlags.EndReached)) == (AircraftLaneFlags.EndOfPath | AircraftLaneFlags.EndReached);
	}

	public static bool ParkingSpaceReached(CarCurrentLane currentLane, PathOwner pathOwner)
	{
		if ((currentLane.m_LaneFlags & (CarLaneFlags.EndReached | CarLaneFlags.ParkingSpace)) == (CarLaneFlags.EndReached | CarLaneFlags.ParkingSpace))
		{
			return (pathOwner.m_State & PathFlags.Pending) == 0;
		}
		return false;
	}

	public static bool ParkingSpaceReached(AircraftCurrentLane currentLane, PathOwner pathOwner)
	{
		if ((currentLane.m_LaneFlags & (AircraftLaneFlags.EndReached | AircraftLaneFlags.ParkingSpace)) == (AircraftLaneFlags.EndReached | AircraftLaneFlags.ParkingSpace))
		{
			return (pathOwner.m_State & PathFlags.Pending) == 0;
		}
		return false;
	}

	public static bool WaypointReached(CarCurrentLane currentLane)
	{
		return (currentLane.m_LaneFlags & (CarLaneFlags.EndReached | CarLaneFlags.Waypoint)) == (CarLaneFlags.EndReached | CarLaneFlags.Waypoint);
	}

	public static bool QueueReached(CarCurrentLane currentLane)
	{
		return (currentLane.m_LaneFlags & (CarLaneFlags.Queue | CarLaneFlags.QueueReached)) == (CarLaneFlags.Queue | CarLaneFlags.QueueReached);
	}

	public static bool RequireNewPath(PathOwner pathOwner)
	{
		if ((pathOwner.m_State & (PathFlags.Obsolete | PathFlags.DivertObsolete)) != 0)
		{
			return (pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) == 0;
		}
		return false;
	}

	public static bool IsStuck(PathOwner pathOwner)
	{
		return (pathOwner.m_State & PathFlags.Stuck) != 0;
	}

	public static void SetTarget(ref PathOwner pathOwner, ref Target targetData, Entity newTarget)
	{
		targetData.m_Target = newTarget;
		pathOwner.m_State &= ~PathFlags.Failed;
		pathOwner.m_State |= PathFlags.Obsolete;
	}

	public static void SetupPathfind(ref CarCurrentLane currentLane, ref PathOwner pathOwner, NativeQueue<SetupQueueItem>.ParallelWriter queue, SetupQueueItem item)
	{
		if ((pathOwner.m_State & (PathFlags.Obsolete | PathFlags.Divert)) == (PathFlags.Obsolete | PathFlags.Divert))
		{
			pathOwner.m_State |= PathFlags.CachedObsolete;
		}
		pathOwner.m_State &= ~(PathFlags.Failed | PathFlags.Obsolete | PathFlags.DivertObsolete);
		pathOwner.m_State |= PathFlags.Pending;
		currentLane.m_LaneFlags &= ~CarLaneFlags.EndOfPath;
		currentLane.m_LaneFlags |= CarLaneFlags.FixedLane;
		queue.Enqueue(item);
	}

	public static void SetupPathfind(ref TrainCurrentLane currentLane, ref PathOwner pathOwner, NativeQueue<SetupQueueItem>.ParallelWriter queue, SetupQueueItem item)
	{
		if ((pathOwner.m_State & (PathFlags.Obsolete | PathFlags.Divert)) == (PathFlags.Obsolete | PathFlags.Divert))
		{
			pathOwner.m_State |= PathFlags.CachedObsolete;
		}
		pathOwner.m_State &= ~(PathFlags.Failed | PathFlags.Obsolete | PathFlags.DivertObsolete);
		pathOwner.m_State |= PathFlags.Pending;
		currentLane.m_Front.m_LaneFlags &= ~TrainLaneFlags.EndOfPath;
		currentLane.m_Rear.m_LaneFlags &= ~TrainLaneFlags.EndOfPath;
		queue.Enqueue(item);
	}

	public static void SetupPathfind(ref WatercraftCurrentLane currentLane, ref PathOwner pathOwner, NativeQueue<SetupQueueItem>.ParallelWriter queue, SetupQueueItem item)
	{
		if ((pathOwner.m_State & (PathFlags.Obsolete | PathFlags.Divert)) == (PathFlags.Obsolete | PathFlags.Divert))
		{
			pathOwner.m_State |= PathFlags.CachedObsolete;
		}
		pathOwner.m_State &= ~(PathFlags.Failed | PathFlags.Obsolete | PathFlags.DivertObsolete);
		pathOwner.m_State |= PathFlags.Pending;
		currentLane.m_LaneFlags &= ~WatercraftLaneFlags.EndOfPath;
		currentLane.m_LaneFlags |= WatercraftLaneFlags.FixedLane;
		queue.Enqueue(item);
	}

	public static void SetupPathfind(ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, NativeQueue<SetupQueueItem>.ParallelWriter queue, SetupQueueItem item)
	{
		if ((pathOwner.m_State & (PathFlags.Obsolete | PathFlags.Divert)) == (PathFlags.Obsolete | PathFlags.Divert))
		{
			pathOwner.m_State |= PathFlags.CachedObsolete;
		}
		pathOwner.m_State &= ~(PathFlags.Failed | PathFlags.Obsolete | PathFlags.DivertObsolete);
		pathOwner.m_State |= PathFlags.Pending;
		currentLane.m_LaneFlags &= ~AircraftLaneFlags.EndOfPath;
		queue.Enqueue(item);
	}

	public static bool ResetUpdatedPath(ref PathOwner pathOwner)
	{
		bool result = (pathOwner.m_State & PathFlags.Updated) != 0;
		pathOwner.m_State &= ~PathFlags.Updated;
		return result;
	}

	public static Game.Objects.Transform CalculateParkingSpaceTarget(Game.Net.ParkingLane parkingLane, ParkingLaneData parkingLaneData, ObjectGeometryData prefabGeometryData, Curve curve, Game.Objects.Transform ownerTransform, float curvePos)
	{
		Game.Objects.Transform result = default(Game.Objects.Transform);
		CalculateParkingSpaceTarget(parkingLane, parkingLaneData, prefabGeometryData, curve, ownerTransform, curvePos, out result.m_Position, out var forward, out var up);
		result.m_Rotation = quaternion.LookRotationSafe(forward, up);
		return result;
	}

	public static void CalculateParkingSpaceTarget(Game.Net.ParkingLane parkingLane, ParkingLaneData parkingLaneData, ObjectGeometryData prefabGeometryData, Curve curve, Game.Objects.Transform ownerTransform, float curvePos, out float3 position, out float3 forward, out float3 up)
	{
		position = MathUtils.Position(curve.m_Bezier, curvePos);
		float3 @float = MathUtils.Tangent(curve.m_Bezier, curvePos);
		@float = math.select(@float, -@float, (parkingLane.m_Flags & ParkingLaneFlags.ParkingInverted) != 0);
		float3 float2 = new float3
		{
			xz = MathUtils.Right(@float.xz)
		};
		if (!ownerTransform.m_Rotation.Equals(default(quaternion)))
		{
			float2.y -= math.dot(float2, math.rotate(ownerTransform.m_Rotation, math.up()));
		}
		float angle = math.select(parkingLaneData.m_SlotAngle, 0f - parkingLaneData.m_SlotAngle, (parkingLane.m_Flags & ParkingLaneFlags.ParkingLeft) != 0);
		up = math.cross(@float, float2);
		forward = math.rotate(quaternion.AxisAngle(math.normalizesafe(up), angle), @float);
		if (parkingLaneData.m_SlotAngle > 0.25f)
		{
			float num = math.max(0f, MathUtils.Size(prefabGeometryData.m_Bounds.z) - parkingLaneData.m_SlotSize.y);
			position += math.normalizesafe(forward) * ((parkingLaneData.m_SlotSize.y + num) * 0.5f - prefabGeometryData.m_Bounds.max.z);
		}
		else
		{
			float falseValue = math.select(0f, -0.5f, (parkingLane.m_Flags & ParkingLaneFlags.ParkingLeft) != 0);
			falseValue = math.select(falseValue, 0.5f, (parkingLane.m_Flags & ParkingLaneFlags.ParkingRight) != 0);
			position += math.normalizesafe(float2) * ((parkingLaneData.m_SlotSize.x - prefabGeometryData.m_Size.x) * falseValue);
		}
	}

	public static Game.Objects.Transform CalculateTransform(Curve curve, float curvePos)
	{
		return new Game.Objects.Transform
		{
			m_Position = MathUtils.Position(curve.m_Bezier, curvePos),
			m_Rotation = quaternion.LookRotationSafe(MathUtils.Tangent(curve.m_Bezier, curvePos), math.up())
		};
	}

	public static int SetParkingCurvePos(Entity entity, ref Unity.Mathematics.Random random, CarCurrentLane currentLane, PathOwner pathOwner, DynamicBuffer<PathElement> path, ref ComponentLookup<ParkedCar> parkedCarData, ref ComponentLookup<Unspawned> unspawnedData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Game.Net.ParkingLane> parkingLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<ObjectGeometryData> prefabObjectGeometryData, ref ComponentLookup<ParkingLaneData> prefabParkingLaneData, ref BufferLookup<LaneObject> laneObjectData, ref BufferLookup<LaneOverlap> laneOverlapData, bool ignoreDriveways)
	{
		for (int i = pathOwner.m_ElementIndex; i < path.Length; i++)
		{
			PathElement pathElement = path[i];
			if (!IsParkingLane(pathElement.m_Target, ref parkingLaneData, ref connectionLaneData))
			{
				continue;
			}
			float curvePos = -1f;
			if (parkingLaneData.HasComponent(pathElement.m_Target))
			{
				float offset;
				float y = GetParkingSize(entity, ref prefabRefData, ref prefabObjectGeometryData, out offset).y;
				if (!FindFreeParkingSpace(ref random, pathElement.m_Target, pathElement.m_TargetDelta.x, y, offset, ref curvePos, ref parkedCarData, ref curveData, ref unspawnedData, ref parkingLaneData, ref prefabRefData, ref prefabParkingLaneData, ref prefabObjectGeometryData, ref laneObjectData, ref laneOverlapData, ignoreDriveways, ignoreDisabled: false))
				{
					curvePos = random.NextFloat(0.05f, 0.95f);
				}
			}
			else
			{
				curvePos = random.NextFloat(0.05f, 0.95f);
			}
			SetParkingCurvePos(path, pathOwner, i, currentLane.m_Lane, curvePos, ref curveData);
			return i;
		}
		return path.Length;
	}

	public static void SetParkingCurvePos(DynamicBuffer<PathElement> path, PathOwner pathOwner, int index, Entity currentLane, float curvePos, ref ComponentLookup<Curve> curveData)
	{
		if (index >= pathOwner.m_ElementIndex)
		{
			PathElement value = path[index];
			value.m_TargetDelta = curvePos;
			path[index] = value;
			currentLane = value.m_Target;
		}
		if (!curveData.TryGetComponent(currentLane, out var componentData))
		{
			return;
		}
		float3 position = MathUtils.Position(componentData.m_Bezier, curvePos);
		if (index > pathOwner.m_ElementIndex)
		{
			PathElement value2 = path[index - 1];
			if (curveData.TryGetComponent(value2.m_Target, out componentData))
			{
				MathUtils.Distance(componentData.m_Bezier, position, out curvePos);
				value2.m_TargetDelta.y = curvePos;
				path[index - 1] = value2;
			}
		}
		if (index < path.Length - 1)
		{
			PathElement value3 = path[index + 1];
			if (curveData.TryGetComponent(value3.m_Target, out componentData))
			{
				MathUtils.Distance(componentData.m_Bezier, position, out curvePos);
				value3.m_TargetDelta.x = curvePos;
				path[index + 1] = value3;
			}
		}
	}

	public static void ResetParkingLaneStatus(Entity entity, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<PathElement> path, ref EntityStorageInfoLookup entityLookup, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Game.Net.ParkingLane> parkingLaneData, ref ComponentLookup<Game.Net.CarLane> carLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<Game.Objects.SpawnLocation> spawnLocationData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<SpawnLocationData> prefabSpawnLocationData)
	{
		if (IsParkingLane(currentLane.m_Lane, ref parkingLaneData, ref connectionLaneData))
		{
			currentLane.m_LaneFlags |= CarLaneFlags.ParkingSpace;
			bool flag = false;
			while (pathOwner.m_ElementIndex < path.Length)
			{
				PathElement pathElement = path[pathOwner.m_ElementIndex];
				if (IsParkingLane(pathElement.m_Target, ref parkingLaneData, ref connectionLaneData))
				{
					SetParkingCurvePos(path, pathOwner, pathOwner.m_ElementIndex++, currentLane.m_Lane, currentLane.m_CurvePosition.z, ref curveData);
					flag = true;
					continue;
				}
				if (!flag)
				{
					SetParkingCurvePos(path, pathOwner, pathOwner.m_ElementIndex - 1, currentLane.m_Lane, currentLane.m_CurvePosition.z, ref curveData);
				}
				if (IsCarLane(pathElement.m_Target, ref carLaneData, ref connectionLaneData, ref spawnLocationData, ref prefabRefData, ref prefabSpawnLocationData) || !entityLookup.Exists(pathElement.m_Target))
				{
					currentLane.m_LaneFlags &= ~CarLaneFlags.ParkingSpace;
				}
				break;
			}
		}
		else if (IsCarLane(currentLane.m_Lane, ref carLaneData, ref connectionLaneData, ref spawnLocationData, ref prefabRefData, ref prefabSpawnLocationData))
		{
			currentLane.m_LaneFlags &= ~CarLaneFlags.ParkingSpace;
		}
	}

	public static void ResetParkingLaneStatus(Entity entity, bool isBicycle, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<PathElement> path, ref EntityStorageInfoLookup entityLookup, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Game.Net.ParkingLane> parkingLaneData, ref ComponentLookup<Game.Net.CarLane> carLaneData, ref ComponentLookup<Game.Net.PedestrianLane> pedestrianLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<Game.Objects.SpawnLocation> spawnLocationData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<SpawnLocationData> prefabSpawnLocationData)
	{
		if (IsParkingLane(currentLane.m_Lane, ref parkingLaneData, ref connectionLaneData))
		{
			currentLane.m_LaneFlags |= CarLaneFlags.ParkingSpace;
			bool flag = false;
			while (pathOwner.m_ElementIndex < path.Length)
			{
				PathElement pathElement = path[pathOwner.m_ElementIndex];
				if (IsParkingLane(pathElement.m_Target, ref parkingLaneData, ref connectionLaneData))
				{
					SetParkingCurvePos(path, pathOwner, pathOwner.m_ElementIndex++, currentLane.m_Lane, currentLane.m_CurvePosition.z, ref curveData);
					flag = true;
					continue;
				}
				if (!flag)
				{
					SetParkingCurvePos(path, pathOwner, pathOwner.m_ElementIndex - 1, currentLane.m_Lane, currentLane.m_CurvePosition.z, ref curveData);
				}
				if (isBicycle && (pathElement.m_Flags & PathElementFlags.Secondary) != 0)
				{
					if (IsPedestrianLaneWithBicycle(pathElement.m_Target, ref pedestrianLaneData, ref connectionLaneData, ref spawnLocationData, ref prefabRefData, ref prefabSpawnLocationData))
					{
						currentLane.m_LaneFlags &= ~CarLaneFlags.ParkingSpace;
					}
				}
				else if (IsCarLane(pathElement.m_Target, ref carLaneData, ref connectionLaneData, ref spawnLocationData, ref prefabRefData, ref prefabSpawnLocationData) || !entityLookup.Exists(pathElement.m_Target))
				{
					currentLane.m_LaneFlags &= ~CarLaneFlags.ParkingSpace;
				}
				break;
			}
		}
		else if (IsCarLane(currentLane.m_Lane, ref carLaneData, ref connectionLaneData, ref spawnLocationData, ref prefabRefData, ref prefabSpawnLocationData) || (isBicycle && IsPedestrianLaneWithBicycle(currentLane.m_Lane, ref pedestrianLaneData, ref connectionLaneData, ref spawnLocationData, ref prefabRefData, ref prefabSpawnLocationData)))
		{
			currentLane.m_LaneFlags &= ~CarLaneFlags.ParkingSpace;
		}
	}

	public static bool IsParkingLane(Entity lane, ref ComponentLookup<Game.Net.ParkingLane> parkingLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData)
	{
		if (parkingLaneData.HasComponent(lane))
		{
			return true;
		}
		if (connectionLaneData.TryGetComponent(lane, out var componentData))
		{
			return (componentData.m_Flags & ConnectionLaneFlags.Parking) != 0;
		}
		return false;
	}

	public static bool IsCarLane(Entity lane, ref ComponentLookup<Game.Net.CarLane> carLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<Game.Objects.SpawnLocation> spawnLocationData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<SpawnLocationData> prefabSpawnLocationData)
	{
		if (carLaneData.HasComponent(lane))
		{
			return true;
		}
		if (connectionLaneData.TryGetComponent(lane, out var componentData))
		{
			return (componentData.m_Flags & ConnectionLaneFlags.Road) != 0;
		}
		if (spawnLocationData.HasComponent(lane) && prefabSpawnLocationData.TryGetComponent(prefabRefData[lane].m_Prefab, out var componentData2))
		{
			if (componentData2.m_ConnectionType != RouteConnectionType.Road)
			{
				return componentData2.m_ConnectionType == RouteConnectionType.Parking;
			}
			return true;
		}
		return false;
	}

	public static bool IsPedestrianLaneWithBicycle(Entity lane, ref ComponentLookup<Game.Net.PedestrianLane> pedestrianLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<Game.Objects.SpawnLocation> spawnLocationData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<SpawnLocationData> prefabSpawnLocationData)
	{
		if (pedestrianLaneData.TryGetComponent(lane, out var componentData))
		{
			return (componentData.m_Flags & PedestrianLaneFlags.AllowBicycle) != 0;
		}
		if (connectionLaneData.TryGetComponent(lane, out var componentData2))
		{
			if ((componentData2.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
			{
				return (componentData2.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) != 0;
			}
			return false;
		}
		if (spawnLocationData.HasComponent(lane) && prefabSpawnLocationData.TryGetComponent(prefabRefData[lane].m_Prefab, out var componentData3))
		{
			if (componentData3.m_ConnectionType == RouteConnectionType.Pedestrian || (componentData3.m_ConnectionType == RouteConnectionType.Parking && componentData3.m_RoadTypes == RoadTypes.Bicycle))
			{
				return componentData3.m_ActivityMask.m_Mask == 0;
			}
			return false;
		}
		return false;
	}

	public static void SetParkingCurvePos(DynamicBuffer<PathElement> path, PathOwner pathOwner, ref CarCurrentLane currentLaneData, DynamicBuffer<CarNavigationLane> navLanes, int navIndex, float curvePos, ref ComponentLookup<Curve> curveData)
	{
		Entity lane = currentLaneData.m_Lane;
		if (navIndex >= 0)
		{
			CarNavigationLane value = navLanes[navIndex];
			value.m_CurvePosition = curvePos;
			navLanes[navIndex] = value;
			lane = value.m_Lane;
		}
		if (!curveData.HasComponent(lane))
		{
			return;
		}
		float3 position = MathUtils.Position(curveData[lane].m_Bezier, curvePos);
		if (navIndex > 0)
		{
			CarNavigationLane value2 = navLanes[navIndex - 1];
			if (curveData.HasComponent(value2.m_Lane))
			{
				MathUtils.Distance(curveData[value2.m_Lane].m_Bezier, position, out curvePos);
				value2.m_CurvePosition.y = curvePos;
				navLanes[navIndex - 1] = value2;
			}
		}
		else if (navIndex == 0 && curveData.HasComponent(currentLaneData.m_Lane))
		{
			MathUtils.Distance(curveData[currentLaneData.m_Lane].m_Bezier, position, out curvePos);
			currentLaneData.m_CurvePosition.z = curvePos;
		}
		if (navIndex < navLanes.Length - 1)
		{
			CarNavigationLane value3 = navLanes[navIndex + 1];
			if (curveData.HasComponent(value3.m_Lane))
			{
				MathUtils.Distance(curveData[value3.m_Lane].m_Bezier, position, out curvePos);
				value3.m_CurvePosition.x = curvePos;
				navLanes[navIndex + 1] = value3;
			}
		}
		else if (navIndex == navLanes.Length - 1 && path.Length > pathOwner.m_ElementIndex)
		{
			PathElement value4 = path[pathOwner.m_ElementIndex];
			if (curveData.HasComponent(value4.m_Target))
			{
				MathUtils.Distance(curveData[value4.m_Target].m_Bezier, position, out curvePos);
				value4.m_TargetDelta.x = curvePos;
				path[pathOwner.m_ElementIndex] = value4;
			}
		}
	}

	public static void CalculateTrainNavigationPivots(Game.Objects.Transform transform, TrainData prefabTrainData, out float3 pivot1, out float3 pivot2)
	{
		float3 @float = math.forward(transform.m_Rotation);
		pivot1 = transform.m_Position + @float * prefabTrainData.m_BogieOffsets.x;
		pivot2 = transform.m_Position - @float * prefabTrainData.m_BogieOffsets.y;
	}

	public static void CalculateShipNavigationPivots(Game.Objects.Transform transform, ObjectGeometryData prefabGeometryData, out float3 pivot1, out float3 pivot2)
	{
		float3 @float = math.forward(transform.m_Rotation) * math.max(1f, (prefabGeometryData.m_Size.z - prefabGeometryData.m_Size.x) * 0.5f);
		pivot1 = transform.m_Position + @float;
		pivot2 = transform.m_Position - @float;
	}

	public static bool CalculateTransformPosition(ref float3 position, Entity entity, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<Position> positions, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<BuildingData> prefabBuildingDatas)
	{
		if (transforms.HasComponent(entity))
		{
			Game.Objects.Transform transform = transforms[entity];
			PrefabRef prefabRef = prefabRefs[entity];
			if (prefabBuildingDatas.HasComponent(prefabRef.m_Prefab))
			{
				BuildingData buildingData = prefabBuildingDatas[prefabRef.m_Prefab];
				position = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
				return true;
			}
			position = transform.m_Position;
			return true;
		}
		if (positions.HasComponent(entity))
		{
			position = positions[entity].m_Position;
			return true;
		}
		return false;
	}

	public static float GetNavigationSize(ObjectGeometryData prefabObjectGeometryData)
	{
		return prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x + 2f;
	}

	public static float GetMaxDriveSpeed(CarData prefabCarData, Game.Net.CarLane carLaneData)
	{
		return GetMaxDriveSpeed(prefabCarData, carLaneData.m_SpeedLimit, carLaneData.m_Curviness);
	}

	public static float GetMaxDriveSpeed(CarData prefabCarData, float speedLimit, float curviness)
	{
		float y = prefabCarData.m_Turning.x * prefabCarData.m_MaxSpeed / math.max(1E-06f, curviness * prefabCarData.m_MaxSpeed + prefabCarData.m_Turning.x - prefabCarData.m_Turning.y);
		y = math.max(1f, y);
		return math.min(speedLimit, y);
	}

	public static void ModifyDriveSpeed(ref float driveSpeed, LaneCondition condition)
	{
		float num = math.saturate((condition.m_Wear - 2.5f) * (2f / 15f));
		driveSpeed *= 1f - num * num * 0.5f;
	}

	public static float GetMaxBrakingSpeed(CarData prefabCarData, float distance, float timeStep)
	{
		float num = timeStep * prefabCarData.m_Braking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabCarData.m_Braking * distance)) - num);
	}

	public static float GetMaxBrakingSpeed(CarData prefabCarData, float distance, float maxResultSpeed, float timeStep)
	{
		float num = timeStep * prefabCarData.m_Braking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabCarData.m_Braking * distance + maxResultSpeed * maxResultSpeed)) - num);
	}

	public static float GetBrakingDistance(CarData prefabCarData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabCarData.m_Braking + speed * timeStep;
	}

	public static Bounds1 CalculateSpeedRange(CarData prefabCarData, float currentSpeed, float timeStep)
	{
		float y = MathUtils.InverseSmoothStep(prefabCarData.m_MaxSpeed, 0f, currentSpeed) * prefabCarData.m_Acceleration;
		float2 @float = currentSpeed + new float2(0f - prefabCarData.m_Braking, y) * timeStep;
		@float.x = math.max(0f, @float.x);
		@float.y = math.min(@float.y, math.max(@float.x, prefabCarData.m_MaxSpeed));
		return new Bounds1(@float.x, @float.y);
	}

	public static int GetPriority(Car carData)
	{
		return math.select(100, 108, (carData.m_Flags & CarFlags.Emergency) != 0);
	}

	public static Game.Net.CarLaneFlags GetForbiddenLaneFlags(Car carData, bool isBicycle)
	{
		Game.Net.CarLaneFlags carLaneFlags = (((carData.m_Flags & CarFlags.UsePublicTransportLanes) == 0) ? Game.Net.CarLaneFlags.PublicOnly : (~(Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.Invert | Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Twoway | Game.Net.CarLaneFlags.IsSecured | Game.Net.CarLaneFlags.Runway | Game.Net.CarLaneFlags.Yield | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.SecondaryStart | Game.Net.CarLaneFlags.SecondaryEnd | Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.PublicOnly | Game.Net.CarLaneFlags.Highway | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward | Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout | Game.Net.CarLaneFlags.RightLimit | Game.Net.CarLaneFlags.LeftLimit | Game.Net.CarLaneFlags.ForbidPassing | Game.Net.CarLaneFlags.RightOfWay | Game.Net.CarLaneFlags.TrafficLights | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight | Game.Net.CarLaneFlags.Forbidden | Game.Net.CarLaneFlags.AllowEnter)));
		return isBicycle ? (Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.Forbidden) : (carLaneFlags | Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.Forbidden);
	}

	public static Game.Net.CarLaneFlags GetPreferredLaneFlags(Car carData)
	{
		if ((carData.m_Flags & CarFlags.PreferPublicTransportLanes) == 0)
		{
			return ~(Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.Invert | Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Twoway | Game.Net.CarLaneFlags.IsSecured | Game.Net.CarLaneFlags.Runway | Game.Net.CarLaneFlags.Yield | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.SecondaryStart | Game.Net.CarLaneFlags.SecondaryEnd | Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.PublicOnly | Game.Net.CarLaneFlags.Highway | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward | Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout | Game.Net.CarLaneFlags.RightLimit | Game.Net.CarLaneFlags.LeftLimit | Game.Net.CarLaneFlags.ForbidPassing | Game.Net.CarLaneFlags.RightOfWay | Game.Net.CarLaneFlags.TrafficLights | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight | Game.Net.CarLaneFlags.Forbidden | Game.Net.CarLaneFlags.AllowEnter);
		}
		return Game.Net.CarLaneFlags.PublicOnly;
	}

	public static float GetSpeedLimitFactor(Car carData)
	{
		return math.select(1f, 2f, (carData.m_Flags & CarFlags.Emergency) != 0);
	}

	public static void GetDrivingStyle(uint simulationFrame, PseudoRandomSeed randomSeed, bool isBicycle, out float safetyTime)
	{
		float x = (float)(simulationFrame & 0xFFF) * 0.0015339808f + randomSeed.GetRandom(PseudoRandomSeed.kDrivingStyle).NextFloat(MathF.PI * 2f);
		float2 @float = math.select(new float2(0.3f, 0.2f), new float2(0.2f, 0.1f), isBicycle);
		safetyTime = @float.x + @float.y * math.sin(x);
	}

	public static float GetMaxDriveSpeed(TrainData prefabTrainData, Game.Net.TrackLane trackLaneData)
	{
		return GetMaxDriveSpeed(prefabTrainData, trackLaneData.m_SpeedLimit, trackLaneData.m_Curviness);
	}

	public static float GetMaxDriveSpeed(TrainData prefabTrainData, float speedLimit, float curviness)
	{
		float y = prefabTrainData.m_Turning.x * prefabTrainData.m_MaxSpeed / math.max(1E-06f, curviness * prefabTrainData.m_MaxSpeed + prefabTrainData.m_Turning.x - prefabTrainData.m_Turning.y);
		y = math.max(1f, y);
		return math.min(speedLimit, y);
	}

	public static float GetMaxBrakingSpeed(TrainData prefabTrainData, float distance, float timeStep)
	{
		float num = timeStep * prefabTrainData.m_Braking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabTrainData.m_Braking * distance)) - num);
	}

	public static float GetMaxBrakingSpeed(TrainData prefabTrainData, float distance, float maxResultSpeed, float timeStep)
	{
		float num = timeStep * prefabTrainData.m_Braking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabTrainData.m_Braking * distance + maxResultSpeed * maxResultSpeed)) - num);
	}

	public static int GetAllBuyingResourcesTrucks(Entity destination, Resource resource, ref ComponentLookup<DeliveryTruck> trucks, ref BufferLookup<GuestVehicle> guestVehiclesBufs, ref BufferLookup<LayoutElement> layoutsBufs)
	{
		int num = 0;
		DynamicBuffer<GuestVehicle> dynamicBuffer = default(DynamicBuffer<GuestVehicle>);
		if (guestVehiclesBufs.HasBuffer(destination))
		{
			dynamicBuffer = guestVehiclesBufs[destination];
		}
		if (dynamicBuffer.IsCreated)
		{
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity vehicle = dynamicBuffer[i].m_Vehicle;
				num += GetBuyingTrucksLoad(vehicle, resource, ref trucks, ref layoutsBufs);
			}
		}
		return num;
	}

	public static int GetBuyingTrucksLoad(Entity vehicle, Resource resource, ref ComponentLookup<DeliveryTruck> trucks, ref BufferLookup<LayoutElement> layouts)
	{
		int num = 0;
		if (trucks.HasComponent(vehicle))
		{
			DeliveryTruck deliveryTruck = trucks[vehicle];
			DynamicBuffer<LayoutElement> dynamicBuffer = default(DynamicBuffer<LayoutElement>);
			if (layouts.HasBuffer(vehicle))
			{
				dynamicBuffer = layouts[vehicle];
			}
			if (dynamicBuffer.IsCreated && layouts[vehicle].Length != 0)
			{
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity vehicle2 = dynamicBuffer[i].m_Vehicle;
					if (trucks.HasComponent(vehicle2))
					{
						DeliveryTruck deliveryTruck2 = trucks[vehicle2];
						if (deliveryTruck2.m_Resource == resource && (deliveryTruck.m_State & DeliveryTruckFlags.Buying) != 0)
						{
							num += deliveryTruck2.m_Amount;
						}
					}
				}
			}
			else if (deliveryTruck.m_Resource == resource && (deliveryTruck.m_State & DeliveryTruckFlags.Buying) != 0)
			{
				num += deliveryTruck.m_Amount;
			}
		}
		return num;
	}

	public static float GetBrakingDistance(TrainData prefabTrainData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabTrainData.m_Braking + speed * timeStep;
	}

	public static float GetSignalDistance(TrainData prefabTrainData, float speed)
	{
		return math.select(0f, speed * 4f, (prefabTrainData.m_TrackType & (TrackTypes.Train | TrackTypes.Subway)) != 0);
	}

	public static Bounds1 CalculateSpeedRange(TrainData prefabTrainData, float currentSpeed, float timeStep)
	{
		float y = MathUtils.InverseSmoothStep(prefabTrainData.m_MaxSpeed, 0f, currentSpeed) * prefabTrainData.m_Acceleration;
		float2 @float = currentSpeed + new float2(0f - prefabTrainData.m_Braking, y) * timeStep;
		@float.x = math.max(0f, @float.x);
		@float.y = math.min(@float.y, math.max(@float.x, prefabTrainData.m_MaxSpeed));
		return new Bounds1(@float.x, @float.y);
	}

	public static int GetPriority(TrainData trainData)
	{
		return math.select(104, 110, (trainData.m_TrackType & (TrackTypes.Train | TrackTypes.Subway)) != 0);
	}

	public static float GetMaxDriveSpeed(WatercraftData prefabWatercraftData, Game.Net.CarLane carLaneData)
	{
		return GetMaxDriveSpeed(prefabWatercraftData, carLaneData.m_SpeedLimit, carLaneData.m_Curviness);
	}

	public static float GetMaxDriveSpeed(WatercraftData prefabWatercraftData, float speedLimit, float curviness)
	{
		float y = prefabWatercraftData.m_Turning.x * prefabWatercraftData.m_MaxSpeed / math.max(1E-06f, curviness * prefabWatercraftData.m_MaxSpeed + prefabWatercraftData.m_Turning.x - prefabWatercraftData.m_Turning.y);
		y = math.max(1f, y);
		return math.min(speedLimit, y);
	}

	public static float GetMaxBrakingSpeed(WatercraftData prefabWatercraftData, float distance, float timeStep)
	{
		float num = timeStep * prefabWatercraftData.m_Braking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabWatercraftData.m_Braking * distance)) - num);
	}

	public static float GetMaxBrakingSpeed(WatercraftData prefabWatercraftData, float distance, float maxResultSpeed, float timeStep)
	{
		float num = timeStep * prefabWatercraftData.m_Braking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabWatercraftData.m_Braking * distance + maxResultSpeed * maxResultSpeed)) - num);
	}

	public static float GetBrakingDistance(WatercraftData prefabWatercraftData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabWatercraftData.m_Braking + speed * timeStep;
	}

	public static Bounds1 CalculateSpeedRange(WatercraftData prefabWatercraftData, float currentSpeed, float timeStep)
	{
		float y = MathUtils.InverseSmoothStep(prefabWatercraftData.m_MaxSpeed, 0f, currentSpeed) * prefabWatercraftData.m_Acceleration;
		float2 @float = currentSpeed + new float2(0f - prefabWatercraftData.m_Braking, y) * timeStep;
		@float.y = math.min(@float.y, math.max(@float.x, prefabWatercraftData.m_MaxSpeed));
		return new Bounds1(@float.x, @float.y);
	}

	public static int GetPriority(WatercraftData prefabWatercraftData)
	{
		return prefabWatercraftData.m_SizeClass switch
		{
			SizeClass.Small => 100, 
			SizeClass.Medium => 102, 
			SizeClass.Large => 104, 
			_ => 100, 
		};
	}

	public static float GetMaxDriveSpeed(AircraftData prefabAircraftData, Game.Net.CarLane carLaneData)
	{
		return GetMaxDriveSpeed(prefabAircraftData, carLaneData.m_SpeedLimit, carLaneData.m_Curviness);
	}

	public static float GetMaxDriveSpeed(AircraftData prefabAircraftData, float speedLimit, float curviness)
	{
		float y = prefabAircraftData.m_GroundTurning.x * prefabAircraftData.m_GroundMaxSpeed / math.max(1E-06f, curviness * prefabAircraftData.m_GroundMaxSpeed + prefabAircraftData.m_GroundTurning.x - prefabAircraftData.m_GroundTurning.y);
		y = math.max(1f, y);
		return math.min(speedLimit, y);
	}

	public static float GetMaxBrakingSpeed(AircraftData prefabAircraftData, float distance, float timeStep)
	{
		float num = timeStep * prefabAircraftData.m_GroundBraking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabAircraftData.m_GroundBraking * distance)) - num);
	}

	public static float GetMaxBrakingSpeed(HelicopterData prefabHelicopterData, float distance, float timeStep)
	{
		float num = timeStep * prefabHelicopterData.m_FlyingAcceleration;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabHelicopterData.m_FlyingAcceleration * distance)) - num);
	}

	public static float GetMaxBrakingSpeed(AirplaneData prefabAirplaneData, float distance, float maxResultSpeed, float timeStep)
	{
		float num = timeStep * prefabAirplaneData.m_FlyingBraking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabAirplaneData.m_FlyingBraking * distance + maxResultSpeed * maxResultSpeed)) - num);
	}

	public static float GetMaxBrakingSpeed(AircraftData prefabAircraftData, float distance, float maxResultSpeed, float timeStep)
	{
		float num = timeStep * prefabAircraftData.m_GroundBraking;
		return math.max(0f, math.sqrt(math.max(0f, num * num + 2f * prefabAircraftData.m_GroundBraking * distance + maxResultSpeed * maxResultSpeed)) - num);
	}

	public static float GetBrakingDistance(AircraftData prefabAircraftData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabAircraftData.m_GroundBraking + speed * timeStep;
	}

	public static float GetBrakingDistance(HelicopterData prefabHelicopterData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabHelicopterData.m_FlyingAcceleration + speed * timeStep;
	}

	public static float GetBrakingDistance(AirplaneData prefabAirplaneData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabAirplaneData.m_FlyingBraking + speed * timeStep;
	}

	public static Bounds1 CalculateSpeedRange(AircraftData prefabAircraftData, float currentSpeed, float timeStep)
	{
		float y = MathUtils.InverseSmoothStep(prefabAircraftData.m_GroundMaxSpeed, 0f, currentSpeed) * prefabAircraftData.m_GroundAcceleration;
		float2 @float = currentSpeed + new float2(0f - prefabAircraftData.m_GroundBraking, y) * timeStep;
		@float.x = math.max(0f, @float.x);
		@float.y = math.min(@float.y, math.max(@float.x, prefabAircraftData.m_GroundMaxSpeed));
		return new Bounds1(@float.x, @float.y);
	}

	public static Bounds1 CalculateSpeedRange(HelicopterData prefabHelicopterData, float currentSpeed, float timeStep)
	{
		float y = MathUtils.InverseSmoothStep(prefabHelicopterData.m_FlyingMaxSpeed, 0f, currentSpeed) * prefabHelicopterData.m_FlyingAcceleration;
		float2 @float = currentSpeed + new float2(0f - prefabHelicopterData.m_FlyingAcceleration, y) * timeStep;
		@float.x = math.max(0f, @float.x);
		@float.y = math.min(@float.y, math.max(@float.x, prefabHelicopterData.m_FlyingMaxSpeed));
		return new Bounds1(@float.x, @float.y);
	}

	public static Bounds1 CalculateSpeedRange(AirplaneData prefabAirplaneData, float currentSpeed, float timeStep)
	{
		float y = MathUtils.InverseSmoothStep(prefabAirplaneData.m_FlyingSpeed.y, 0f, currentSpeed) * prefabAirplaneData.m_FlyingAcceleration;
		float2 @float = currentSpeed + new float2(0f - prefabAirplaneData.m_FlyingBraking, y) * timeStep;
		@float = new float2(math.max(@float.x, math.min(@float.y, prefabAirplaneData.m_FlyingSpeed.x)), math.min(@float.y, math.max(@float.x, prefabAirplaneData.m_FlyingSpeed.y)));
		return new Bounds1(@float.x, @float.y);
	}

	public static int GetPriority(AircraftData prefabAircraftData)
	{
		return 104;
	}

	public static void DeleteVehicle(EntityCommandBuffer commandBuffer, Entity vehicle, DynamicBuffer<LayoutElement> layout)
	{
		if (layout.IsCreated && layout.Length != 0)
		{
			for (int i = 0; i < layout.Length; i++)
			{
				commandBuffer.AddComponent(layout[i].m_Vehicle, default(Deleted));
			}
		}
		else
		{
			commandBuffer.AddComponent(vehicle, default(Deleted));
		}
	}

	public static void DeleteVehicle(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, Entity vehicle, DynamicBuffer<LayoutElement> layout)
	{
		if (layout.IsCreated && layout.Length != 0)
		{
			for (int i = 0; i < layout.Length; i++)
			{
				commandBuffer.AddComponent(jobIndex, layout[i].m_Vehicle, default(Deleted));
			}
		}
		else
		{
			commandBuffer.AddComponent(jobIndex, vehicle, default(Deleted));
		}
	}

	public static float2 GetParkingSize(ParkingLaneData parkingLaneData)
	{
		float num = math.select(parkingLaneData.m_SlotSize.x, parkingLaneData.m_SlotSize.x * 3f, parkingLaneData.m_RoadTypes == RoadTypes.Bicycle);
		return math.select(new float2(num, parkingLaneData.m_MaxCarLength), new float2(num * 1.25f, 1000000f), new bool2(parkingLaneData.m_SlotAngle < 0.01f, parkingLaneData.m_MaxCarLength == 0f));
	}

	public static Entity GetParkingSource(Entity entity, CarCurrentLane currentLane, ref ComponentLookup<Game.Net.ParkingLane> parkingLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData)
	{
		if (parkingLaneData.HasComponent(currentLane.m_Lane))
		{
			return currentLane.m_Lane;
		}
		if (connectionLaneData.TryGetComponent(currentLane.m_Lane, out var componentData) && (componentData.m_Flags & ConnectionLaneFlags.Parking) != 0)
		{
			return currentLane.m_Lane;
		}
		return entity;
	}

	public static float2 GetParkingSize(Entity car, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<ObjectGeometryData> objectGeometryData)
	{
		float offset;
		return GetParkingSize(car, ref prefabRefData, ref objectGeometryData, out offset);
	}

	public static float2 GetParkingSize(Entity car, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<ObjectGeometryData> objectGeometryData, out float offset)
	{
		if (prefabRefData.TryGetComponent(car, out var componentData) && objectGeometryData.TryGetComponent(componentData.m_Prefab, out var componentData2))
		{
			return GetParkingSize(componentData2, out offset);
		}
		offset = 0f;
		return 0f;
	}

	public static float2 GetParkingSize(ObjectGeometryData objectGeometry, out float offset)
	{
		offset = 0f - MathUtils.Center(objectGeometry.m_Bounds.z);
		return math.max(new float2(0.01f, 1.01f), MathUtils.Size(objectGeometry.m_Bounds.xz));
	}

	public static float2 GetParkingOffsets(Entity car, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<ObjectGeometryData> objectGeometryData)
	{
		if (prefabRefData.TryGetComponent(car, out var componentData) && objectGeometryData.TryGetComponent(componentData.m_Prefab, out var componentData2))
		{
			return math.max(0.1f, new float2(0f - componentData2.m_Bounds.min.z, componentData2.m_Bounds.max.z));
		}
		return 0f;
	}

	public static RuleFlags GetIgnoredPathfindRules(CarData carData)
	{
		RuleFlags ruleFlags = RuleFlags.AvoidBicycles;
		if ((int)carData.m_SizeClass < 2)
		{
			ruleFlags |= RuleFlags.ForbidHeavyTraffic;
		}
		if ((carData.m_EnergyType & EnergyTypes.Fuel) == 0)
		{
			ruleFlags |= RuleFlags.ForbidCombustionEngines;
		}
		if (carData.m_MaxSpeed >= 22.222223f)
		{
			ruleFlags |= RuleFlags.ForbidSlowTraffic;
		}
		return ruleFlags;
	}

	public static RuleFlags GetIgnoredPathfindRulesTaxiDefaults()
	{
		return RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles;
	}

	public static RuleFlags GetIgnoredPathfindRulesBicycleDefaults()
	{
		return RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic;
	}

	public static bool IsReversedPath(DynamicBuffer<PathElement> path, PathOwner pathOwner, Entity vehicle, DynamicBuffer<LayoutElement> layout, ComponentLookup<Curve> curveData, ComponentLookup<TrainCurrentLane> currentLaneData, ComponentLookup<Train> trainData, ComponentLookup<Game.Objects.Transform> transformData)
	{
		if (path.Length <= pathOwner.m_ElementIndex)
		{
			return false;
		}
		PathElement pathElement = path[pathOwner.m_ElementIndex];
		if (!curveData.HasComponent(pathElement.m_Target))
		{
			return false;
		}
		float3 x = MathUtils.Position(curveData[pathElement.m_Target].m_Bezier, pathElement.m_TargetDelta.x);
		Entity entity = vehicle;
		Entity entity2 = vehicle;
		if (layout.Length != 0)
		{
			entity = layout[0].m_Vehicle;
			entity2 = layout[layout.Length - 1].m_Vehicle;
		}
		TrainCurrentLane trainCurrentLane = currentLaneData[entity];
		TrainCurrentLane trainCurrentLane2 = currentLaneData[entity2];
		float3 y;
		float3 y2;
		if ((trainCurrentLane.m_Front.m_Lane != trainCurrentLane2.m_Rear.m_Lane || trainCurrentLane.m_Front.m_CurvePosition.w != trainCurrentLane2.m_Rear.m_CurvePosition.y) && curveData.HasComponent(trainCurrentLane.m_Front.m_Lane) && curveData.HasComponent(trainCurrentLane2.m_Rear.m_Lane))
		{
			Curve curve = curveData[trainCurrentLane.m_Front.m_Lane];
			Curve curve2 = curveData[trainCurrentLane2.m_Rear.m_Lane];
			y = MathUtils.Position(curve.m_Bezier, trainCurrentLane.m_Front.m_CurvePosition.w);
			y2 = MathUtils.Position(curve2.m_Bezier, trainCurrentLane2.m_Rear.m_CurvePosition.y);
		}
		else
		{
			Train train = trainData[entity];
			Train train2 = trainData[entity2];
			Game.Objects.Transform transform = transformData[entity];
			Game.Objects.Transform transform2 = transformData[entity2];
			float3 @float = math.forward(transform.m_Rotation);
			float3 float2 = math.forward(transform2.m_Rotation);
			@float = math.select(@float, -@float, (train.m_Flags & TrainFlags.Reversed) != 0);
			float2 = math.select(float2, -float2, (train2.m_Flags & TrainFlags.Reversed) != 0);
			y = transform.m_Position + @float;
			y2 = transform2.m_Position - float2;
		}
		return math.distancesq(x, y) > math.distancesq(x, y2);
	}

	public static void ReverseTrain(Entity vehicle, DynamicBuffer<LayoutElement> layout, ref ComponentLookup<Train> trainData, ref ComponentLookup<TrainCurrentLane> currentLaneData, ref ComponentLookup<TrainNavigation> navigationData)
	{
		if (layout.Length != 0)
		{
			TrainCurrentLane trainCurrentLane = currentLaneData[layout[0].m_Vehicle];
			TrainCurrentLane trainCurrentLane2 = currentLaneData[layout[layout.Length - 1].m_Vehicle];
			TrainBogieCache rearCache = new TrainBogieCache(trainCurrentLane.m_Front);
			TrainFlags trainFlags = trainData[layout[0].m_Vehicle].m_Flags & TrainFlags.IgnoreParkedVehicle;
			for (int i = 0; i < layout.Length; i++)
			{
				ReverseCarriage(layout[i].m_Vehicle, trainCurrentLane2.m_Rear.m_Lane, trainCurrentLane2.m_Rear.m_CurvePosition.y, ref trainData, ref currentLaneData, ref navigationData, ref rearCache);
			}
			CollectionUtils.Reverse(layout.AsNativeArray());
			Train value = trainData[layout[0].m_Vehicle];
			value.m_Flags |= trainFlags;
			trainData[layout[0].m_Vehicle] = value;
		}
		else
		{
			TrainCurrentLane trainCurrentLane3 = currentLaneData[vehicle];
			TrainBogieCache rearCache2 = new TrainBogieCache(trainCurrentLane3.m_Front);
			ReverseCarriage(vehicle, trainCurrentLane3.m_Rear.m_Lane, trainCurrentLane3.m_Rear.m_CurvePosition.y, ref trainData, ref currentLaneData, ref navigationData, ref rearCache2);
		}
	}

	public static void ReverseCarriage(Entity vehicle, Entity lastLane, float lastCurvePos, ref ComponentLookup<Train> trainData, ref ComponentLookup<TrainCurrentLane> currentLaneData, ref ComponentLookup<TrainNavigation> navigationData, ref TrainBogieCache rearCache)
	{
		Train value = trainData[vehicle];
		TrainCurrentLane value2 = currentLaneData[vehicle];
		TrainNavigation value3 = navigationData[vehicle];
		value.m_Flags &= ~TrainFlags.IgnoreParkedVehicle;
		value.m_Flags ^= TrainFlags.Reversed;
		CommonUtils.Swap(ref value2.m_Front, ref value2.m_Rear);
		CommonUtils.Swap(ref value2.m_RearCache, ref rearCache);
		value2.m_Front.m_CurvePosition = value2.m_Front.m_CurvePosition.wyyx;
		value2.m_FrontCache.m_CurvePosition = value2.m_FrontCache.m_CurvePosition.yx;
		value2.m_Rear.m_CurvePosition = value2.m_Rear.m_CurvePosition.wyyx;
		value2.m_RearCache.m_CurvePosition = value2.m_RearCache.m_CurvePosition.yx;
		if (value2.m_Front.m_Lane == lastLane)
		{
			value2.m_Front.m_CurvePosition.w = lastCurvePos;
		}
		if (value2.m_FrontCache.m_Lane == lastLane)
		{
			value2.m_FrontCache.m_CurvePosition.y = lastCurvePos;
		}
		if (value2.m_Rear.m_Lane == lastLane)
		{
			value2.m_Rear.m_CurvePosition.w = lastCurvePos;
		}
		if (value2.m_RearCache.m_Lane == lastLane)
		{
			value2.m_RearCache.m_CurvePosition.y = lastCurvePos;
		}
		CommonUtils.Swap(ref value3.m_Front, ref value3.m_Rear);
		value3.m_Front.m_Direction = -value3.m_Front.m_Direction;
		value3.m_Rear.m_Direction = -value3.m_Rear.m_Direction;
		value2.m_Front.m_LaneFlags &= ~TrainLaneFlags.Return;
		value2.m_FrontCache.m_LaneFlags &= ~TrainLaneFlags.Return;
		value2.m_Rear.m_LaneFlags &= ~TrainLaneFlags.Return;
		value2.m_RearCache.m_LaneFlags &= ~TrainLaneFlags.Return;
		trainData[vehicle] = value;
		currentLaneData[vehicle] = value2;
		navigationData[vehicle] = value3;
	}

	public static float CalculateLength(Entity vehicle, DynamicBuffer<LayoutElement> layout, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<TrainData> prefabTrainData)
	{
		if (layout.Length != 0)
		{
			float num = 0f;
			for (int i = 0; i < layout.Length; i++)
			{
				num += math.csum(prefabTrainData[prefabRefData[layout[i].m_Vehicle].m_Prefab].m_AttachOffsets);
			}
			return num;
		}
		return math.csum(prefabTrainData[prefabRefData[vehicle].m_Prefab].m_AttachOffsets);
	}

	public static void UpdateCarriageLocations(DynamicBuffer<LayoutElement> layout, NativeList<PathElement> laneBuffer, ref ComponentLookup<Train> trainData, ref ComponentLookup<ParkedTrain> parkedTrainData, ref ComponentLookup<TrainCurrentLane> currentLaneData, ref ComponentLookup<TrainNavigation> navigationData, ref ComponentLookup<Game.Objects.Transform> transformData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<TrainData> prefabTrainData)
	{
		if (laneBuffer.Length == 0)
		{
			return;
		}
		int num = 0;
		PathElement pathElement = laneBuffer[num++];
		float y = pathElement.m_TargetDelta.y;
		float3 @float = MathUtils.Position(curveData[pathElement.m_Target].m_Bezier, pathElement.m_TargetDelta.y);
		float num2 = 0f;
		Game.Objects.Transform value4 = default(Game.Objects.Transform);
		for (int i = 0; i < layout.Length; i++)
		{
			Entity vehicle = layout[i].m_Vehicle;
			Train train = trainData[vehicle];
			TrainData trainData2 = prefabTrainData[prefabRefData[vehicle].m_Prefab];
			bool flag = (train.m_Flags & TrainFlags.Reversed) != 0;
			if (flag)
			{
				trainData2.m_BogieOffsets = trainData2.m_BogieOffsets.yx;
				trainData2.m_AttachOffsets = trainData2.m_AttachOffsets.yx;
			}
			TrainCurrentLane value = default(TrainCurrentLane);
			TrainNavigation value2 = default(TrainNavigation);
			num2 += trainData2.m_AttachOffsets.x - trainData2.m_BogieOffsets.x;
			while (true)
			{
				Curve curve = curveData[pathElement.m_Target];
				if (MoveBufferPosition(@float, ref value2.m_Front, num2, curve.m_Bezier, ref pathElement.m_TargetDelta) || num >= laneBuffer.Length)
				{
					break;
				}
				pathElement = laneBuffer[num++];
				y = pathElement.m_TargetDelta.y;
			}
			TrainLaneFlags trainLaneFlags = (TrainLaneFlags)0u;
			if (connectionLaneData.HasComponent(pathElement.m_Target))
			{
				trainLaneFlags |= TrainLaneFlags.Connection;
			}
			value.m_Front = new TrainBogieLane(pathElement.m_Target, new float4(pathElement.m_TargetDelta.xyy, y), trainLaneFlags);
			value.m_FrontCache = new TrainBogieCache(value.m_Front);
			if (i != 0)
			{
				ClampPosition(ref value2.m_Front.m_Position, @float, num2);
			}
			@float = value2.m_Front.m_Position;
			num2 = math.csum(trainData2.m_BogieOffsets);
			while (true)
			{
				Curve curve = curveData[pathElement.m_Target];
				if (MoveBufferPosition(@float, ref value2.m_Rear, num2, curve.m_Bezier, ref pathElement.m_TargetDelta) || num >= laneBuffer.Length)
				{
					break;
				}
				pathElement = laneBuffer[num++];
				y = pathElement.m_TargetDelta.y;
			}
			TrainLaneFlags trainLaneFlags2 = (TrainLaneFlags)0u;
			if (connectionLaneData.HasComponent(pathElement.m_Target))
			{
				trainLaneFlags2 |= TrainLaneFlags.Connection;
			}
			value.m_Rear = new TrainBogieLane(pathElement.m_Target, new float4(pathElement.m_TargetDelta.xyy, y), trainLaneFlags2);
			value.m_RearCache = new TrainBogieCache(value.m_Rear);
			ClampPosition(ref value2.m_Rear.m_Position, @float, num2);
			@float = value2.m_Rear.m_Position;
			num2 = trainData2.m_AttachOffsets.y - trainData2.m_BogieOffsets.y;
			float3 value3 = value2.m_Rear.m_Position - value2.m_Front.m_Position;
			MathUtils.TryNormalize(ref value3, trainData2.m_BogieOffsets.x);
			value4.m_Position = value2.m_Front.m_Position + value3;
			float3 value5 = math.select(-value3, value3, flag);
			if (MathUtils.TryNormalize(ref value5))
			{
				value4.m_Rotation = quaternion.LookRotationSafe(value5, math.up());
			}
			else
			{
				value4.m_Rotation = quaternion.identity;
			}
			transformData[vehicle] = value4;
			if (parkedTrainData.TryGetComponent(vehicle, out var componentData))
			{
				componentData.m_FrontLane = value.m_Front.m_Lane;
				componentData.m_RearLane = value.m_Rear.m_Lane;
				componentData.m_CurvePosition = new float2(value.m_Front.m_CurvePosition.y, value.m_Rear.m_CurvePosition.y);
				parkedTrainData[vehicle] = componentData;
			}
			else if (currentLaneData.HasComponent(vehicle))
			{
				currentLaneData[vehicle] = value;
				navigationData[vehicle] = value2;
			}
		}
	}

	private static void ClampPosition(ref float3 position, float3 original, float maxDistance)
	{
		position = original + MathUtils.ClampLength(position - original, maxDistance);
	}

	private static bool MoveBufferPosition(float3 comparePosition, ref TrainBogiePosition targetPosition, float minDistance, Bezier4x3 curve, ref float2 curveDelta)
	{
		float3 @float = MathUtils.Position(curve, curveDelta.x);
		if (math.distance(comparePosition, @float) < minDistance)
		{
			curveDelta.y = curveDelta.x;
			targetPosition.m_Position = @float;
			targetPosition.m_Direction = MathUtils.Tangent(curve, curveDelta.x);
			targetPosition.m_Direction *= math.sign(curveDelta.y - curveDelta.x);
			return false;
		}
		float2 float2 = curveDelta;
		for (int i = 0; i < 8; i++)
		{
			float num = math.lerp(float2.x, float2.y, 0.5f);
			float3 y = MathUtils.Position(curve, num);
			if (math.distance(comparePosition, y) < minDistance)
			{
				float2.y = num;
			}
			else
			{
				float2.x = num;
			}
		}
		curveDelta.y = float2.x;
		targetPosition.m_Position = MathUtils.Position(curve, float2.x);
		targetPosition.m_Direction = MathUtils.Tangent(curve, float2.x);
		targetPosition.m_Direction *= math.sign(curveDelta.y - curveDelta.x);
		return true;
	}

	public static void ClearEndOfPath(ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes)
	{
		if (navigationLanes.Length != 0)
		{
			CarNavigationLane value = navigationLanes[navigationLanes.Length - 1];
			value.m_Flags &= ~CarLaneFlags.EndOfPath;
			navigationLanes[navigationLanes.Length - 1] = value;
		}
		else
		{
			currentLane.m_LaneFlags &= ~CarLaneFlags.EndOfPath;
		}
	}

	public static void ClearEndOfPath(ref WatercraftCurrentLane currentLane, DynamicBuffer<WatercraftNavigationLane> navigationLanes)
	{
		if (navigationLanes.Length != 0)
		{
			WatercraftNavigationLane value = navigationLanes[navigationLanes.Length - 1];
			value.m_Flags &= ~WatercraftLaneFlags.EndOfPath;
			navigationLanes[navigationLanes.Length - 1] = value;
		}
		else
		{
			currentLane.m_LaneFlags &= ~WatercraftLaneFlags.EndOfPath;
		}
	}

	public static void ClearEndOfPath(ref AircraftCurrentLane currentLane, DynamicBuffer<AircraftNavigationLane> navigationLanes)
	{
		if (navigationLanes.Length != 0)
		{
			AircraftNavigationLane value = navigationLanes[navigationLanes.Length - 1];
			value.m_Flags &= ~AircraftLaneFlags.EndOfPath;
			navigationLanes[navigationLanes.Length - 1] = value;
		}
		else
		{
			currentLane.m_LaneFlags &= ~AircraftLaneFlags.EndOfPath;
		}
	}

	public static void ClearEndOfPath(ref TrainCurrentLane currentLane, DynamicBuffer<TrainNavigationLane> navigationLanes)
	{
		while (navigationLanes.Length != 0)
		{
			TrainNavigationLane value = navigationLanes[navigationLanes.Length - 1];
			if ((value.m_Flags & TrainLaneFlags.ParkingSpace) != 0)
			{
				navigationLanes.RemoveAt(navigationLanes.Length - 1);
				continue;
			}
			value.m_Flags &= ~TrainLaneFlags.EndOfPath;
			navigationLanes[navigationLanes.Length - 1] = value;
			return;
		}
		currentLane.m_Front.m_LaneFlags &= ~TrainLaneFlags.EndOfPath;
	}

	public static Entity ValidateParkingSpace(Entity entity, ref Unity.Mathematics.Random random, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<PathElement> path, ref ComponentLookup<ParkedCar> parkedCarData, ref ComponentLookup<Blocker> blockerData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Unspawned> unspawnedData, ref ComponentLookup<Game.Net.ParkingLane> parkingLaneData, ref ComponentLookup<GarageLane> garageLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<ParkingLaneData> prefabParkingLaneData, ref ComponentLookup<ObjectGeometryData> prefabObjectGeometryData, ref BufferLookup<LaneObject> laneObjectData, ref BufferLookup<LaneOverlap> laneOverlapData, bool ignoreDriveways, bool ignoreDisabled, bool boardingOnly)
	{
		if ((currentLane.m_LaneFlags & CarLaneFlags.ParkingSpace) != 0)
		{
			float offset;
			float y = GetParkingSize(entity, ref prefabRefData, ref prefabObjectGeometryData, out offset).y;
			float curvePos = currentLane.m_CurvePosition.z;
			GarageLane componentData;
			if (parkingLaneData.HasComponent(currentLane.m_Lane))
			{
				if (FindFreeParkingSpace(ref random, currentLane.m_Lane, 0f, y, offset, ref curvePos, ref parkedCarData, ref curveData, ref unspawnedData, ref parkingLaneData, ref prefabRefData, ref prefabParkingLaneData, ref prefabObjectGeometryData, ref laneObjectData, ref laneOverlapData, ignoreDriveways, ignoreDisabled))
				{
					if (curvePos != currentLane.m_CurvePosition.z)
					{
						currentLane.m_CurvePosition.z = curvePos;
						SetParkingCurvePos(path, pathOwner, ref currentLane, navigationLanes, -1, curvePos, ref curveData);
					}
				}
				else
				{
					pathOwner.m_State |= PathFlags.Obsolete;
				}
			}
			else if (!boardingOnly && garageLaneData.TryGetComponent(currentLane.m_Lane, out componentData))
			{
				Game.Net.ConnectionLane connectionLane = connectionLaneData[currentLane.m_Lane];
				if (componentData.m_VehicleCount >= componentData.m_VehicleCapacity || (!ignoreDisabled && (connectionLane.m_Flags & ConnectionLaneFlags.Disabled) != 0))
				{
					pathOwner.m_State |= PathFlags.Obsolete;
				}
			}
			return currentLane.m_Lane;
		}
		for (int i = 0; i < navigationLanes.Length; i++)
		{
			CarNavigationLane value = navigationLanes[i];
			if ((value.m_Flags & CarLaneFlags.ParkingSpace) == 0)
			{
				continue;
			}
			GarageLane componentData4;
			if (parkingLaneData.HasComponent(value.m_Lane))
			{
				float minT;
				if (i == 0)
				{
					minT = currentLane.m_CurvePosition.y;
				}
				else
				{
					CarNavigationLane carNavigationLane = navigationLanes[i - 1];
					minT = (((carNavigationLane.m_Flags & CarLaneFlags.Reserved) == 0) ? carNavigationLane.m_CurvePosition.x : carNavigationLane.m_CurvePosition.y);
				}
				float offset2;
				float y2 = GetParkingSize(entity, ref prefabRefData, ref prefabObjectGeometryData, out offset2).y;
				float curvePos2 = value.m_CurvePosition.x;
				if (FindFreeParkingSpace(ref random, value.m_Lane, minT, y2, offset2, ref curvePos2, ref parkedCarData, ref curveData, ref unspawnedData, ref parkingLaneData, ref prefabRefData, ref prefabParkingLaneData, ref prefabObjectGeometryData, ref laneObjectData, ref laneOverlapData, ignoreDriveways, ignoreDisabled))
				{
					if ((value.m_Flags & CarLaneFlags.Validated) == 0)
					{
						value.m_Flags |= CarLaneFlags.Validated;
						navigationLanes[i] = value;
					}
					if (curvePos2 != value.m_CurvePosition.x)
					{
						SetParkingCurvePos(path, pathOwner, ref currentLane, navigationLanes, i, curvePos2, ref curveData);
					}
				}
				else
				{
					if ((value.m_Flags & CarLaneFlags.Validated) != 0)
					{
						value.m_Flags &= ~CarLaneFlags.Validated;
						navigationLanes[i] = value;
					}
					if (boardingOnly)
					{
						Blocker componentData2;
						ParkedCar componentData3;
						if (i == 0)
						{
							currentLane.m_LaneFlags |= CarLaneFlags.EndOfPath;
							navigationLanes.Clear();
						}
						else if (blockerData.TryGetComponent(entity, out componentData2) && parkedCarData.TryGetComponent(componentData2.m_Blocker, out componentData3) && componentData3.m_Lane == value.m_Lane)
						{
							CarNavigationLane value2 = navigationLanes[i - 1];
							value2.m_Flags |= CarLaneFlags.EndOfPath;
							navigationLanes[i - 1] = value2;
							navigationLanes.RemoveRange(i, navigationLanes.Length - i);
						}
					}
					else
					{
						if (i == 0)
						{
							currentLane.m_CurvePosition.z = 1f;
						}
						pathOwner.m_State |= PathFlags.Obsolete;
					}
				}
			}
			else if (!boardingOnly && garageLaneData.TryGetComponent(value.m_Lane, out componentData4))
			{
				Game.Net.ConnectionLane connectionLane2 = connectionLaneData[value.m_Lane];
				if (componentData4.m_VehicleCount < componentData4.m_VehicleCapacity && (ignoreDisabled || (connectionLane2.m_Flags & ConnectionLaneFlags.Disabled) == 0))
				{
					if ((value.m_Flags & CarLaneFlags.Validated) == 0)
					{
						value.m_Flags |= CarLaneFlags.Validated;
						navigationLanes[i] = value;
					}
				}
				else
				{
					if ((value.m_Flags & CarLaneFlags.Validated) != 0)
					{
						value.m_Flags &= ~CarLaneFlags.Validated;
						navigationLanes[i] = value;
					}
					pathOwner.m_State |= PathFlags.Obsolete;
				}
			}
			else if ((value.m_Flags & CarLaneFlags.Validated) == 0)
			{
				value.m_Flags |= CarLaneFlags.Validated;
				navigationLanes[i] = value;
			}
			return value.m_Lane;
		}
		return Entity.Null;
	}

	public static bool FindFreeParkingSpace(ref Unity.Mathematics.Random random, Entity lane, float minT, float parkingLength, float parkingOffset, ref float curvePos, ref ComponentLookup<ParkedCar> parkedCarData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Unspawned> unspawnedData, ref ComponentLookup<Game.Net.ParkingLane> parkingLaneData, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<ParkingLaneData> prefabParkingLaneData, ref ComponentLookup<ObjectGeometryData> prefabObjectGeometryData, ref BufferLookup<LaneObject> laneObjectData, ref BufferLookup<LaneOverlap> laneOverlapData, bool ignoreDriveways, bool ignoreDisabled)
	{
		Curve curve = curveData[lane];
		Game.Net.ParkingLane parkingLane = parkingLaneData[lane];
		if ((parkingLane.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
		{
			return false;
		}
		if (ignoreDisabled && (parkingLane.m_Flags & ParkingLaneFlags.ParkingDisabled) != 0)
		{
			return false;
		}
		PrefabRef prefabRef = prefabRefData[lane];
		DynamicBuffer<LaneObject> dynamicBuffer = laneObjectData[lane];
		DynamicBuffer<LaneOverlap> dynamicBuffer2 = default(DynamicBuffer<LaneOverlap>);
		if (!ignoreDriveways)
		{
			dynamicBuffer2 = laneOverlapData[lane];
		}
		ParkingLaneData prefabParkingLane = prefabParkingLaneData[prefabRef.m_Prefab];
		if (prefabParkingLane.m_MaxCarLength != 0f && prefabParkingLane.m_MaxCarLength < parkingLength)
		{
			return false;
		}
		if (prefabParkingLane.m_SlotInterval != 0f)
		{
			int parkingSlotCount = NetUtils.GetParkingSlotCount(curve, parkingLane, prefabParkingLane);
			float parkingSlotInterval = NetUtils.GetParkingSlotInterval(curve, parkingLane, prefabParkingLane, parkingSlotCount);
			float3 x = curve.m_Bezier.a;
			float2 @float = 0f;
			float num = 0f;
			float num2 = math.max((parkingLane.m_Flags & (ParkingLaneFlags.StartingLane | ParkingLaneFlags.EndingLane)) switch
			{
				ParkingLaneFlags.StartingLane => curve.m_Length - (float)parkingSlotCount * parkingSlotInterval, 
				ParkingLaneFlags.EndingLane => 0f, 
				_ => (curve.m_Length - (float)parkingSlotCount * parkingSlotInterval) * 0.5f, 
			}, 0f);
			int i = -1;
			int num3 = 0;
			float num4 = curvePos;
			float num5 = 2f;
			int num6 = 0;
			while (num6 < dynamicBuffer.Length)
			{
				LaneObject laneObject = dynamicBuffer[num6++];
				if (parkedCarData.HasComponent(laneObject.m_LaneObject) && !unspawnedData.HasComponent(laneObject.m_LaneObject))
				{
					num5 = laneObject.m_CurvePosition.x;
					break;
				}
			}
			float2 float2 = 2f;
			int num7 = 0;
			if (!ignoreDriveways && num7 < dynamicBuffer2.Length)
			{
				LaneOverlap laneOverlap = dynamicBuffer2[num7++];
				float2 = new float2((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd) * 0.003921569f;
			}
			for (int j = 1; j <= 16; j++)
			{
				float num8 = (float)j * 0.0625f;
				float3 float3 = MathUtils.Position(curve.m_Bezier, num8);
				for (num += math.distance(x, float3); num >= num2 || (j == 16 && i < parkingSlotCount); i++)
				{
					@float.y = math.select(num8, math.lerp(@float.x, num8, num2 / num), num2 < num);
					bool flag = false;
					if (num5 <= @float.y)
					{
						num5 = 2f;
						flag = true;
						while (num6 < dynamicBuffer.Length)
						{
							LaneObject laneObject2 = dynamicBuffer[num6++];
							if (parkedCarData.HasComponent(laneObject2.m_LaneObject) && !unspawnedData.HasComponent(laneObject2.m_LaneObject) && laneObject2.m_CurvePosition.x > @float.y)
							{
								num5 = laneObject2.m_CurvePosition.x;
								break;
							}
						}
					}
					if (!ignoreDriveways && float2.x < @float.y)
					{
						flag = true;
						if (float2.y <= @float.y)
						{
							float2 = 2f;
							while (num7 < dynamicBuffer2.Length)
							{
								LaneOverlap laneOverlap2 = dynamicBuffer2[num7++];
								float2 float4 = new float2((int)laneOverlap2.m_ThisStart, (int)laneOverlap2.m_ThisEnd) * 0.003921569f;
								if (float4.y > @float.y)
								{
									float2 = float4;
									break;
								}
							}
						}
					}
					if (!flag && i >= 0 && i < parkingSlotCount)
					{
						if (curvePos >= @float.x && curvePos <= @float.y)
						{
							curvePos = math.lerp(@float.x, @float.y, 0.5f);
							return true;
						}
						if (@float.y > minT)
						{
							num3++;
							if (random.NextInt(num3) == 0)
							{
								num4 = math.lerp(@float.x, @float.y, 0.5f);
							}
						}
					}
					num -= num2;
					@float.x = @float.y;
					num2 = parkingSlotInterval;
				}
				x = float3;
			}
			if (num4 != curvePos && prefabParkingLane.m_SlotAngle <= 0.25f)
			{
				if (parkingOffset > 0f)
				{
					Bounds1 t = new Bounds1(num4, 1f);
					MathUtils.ClampLength(curve.m_Bezier, ref t, parkingOffset);
					num4 = t.max;
				}
				else if (parkingOffset < 0f)
				{
					Bounds1 t2 = new Bounds1(0f, num4);
					MathUtils.ClampLengthInverse(curve.m_Bezier, ref t2, 0f - parkingOffset);
					num4 = t2.min;
				}
			}
			curvePos = num4;
			return num3 != 0;
		}
		float2 float5 = default(float2);
		float2 float6 = default(float2);
		int num9 = 0;
		float3 float7 = default(float3);
		float2 float8 = math.select(0f, 0.5f, (parkingLane.m_Flags & ParkingLaneFlags.StartingLane) == 0);
		float3 x2 = curve.m_Bezier.a;
		float num10 = 2f;
		float2 float9 = 0f;
		int num11 = 0;
		while (num11 < dynamicBuffer.Length)
		{
			LaneObject laneObject3 = dynamicBuffer[num11++];
			if (parkedCarData.HasComponent(laneObject3.m_LaneObject) && !unspawnedData.HasComponent(laneObject3.m_LaneObject))
			{
				num10 = laneObject3.m_CurvePosition.x;
				float9 = GetParkingOffsets(laneObject3.m_LaneObject, ref prefabRefData, ref prefabObjectGeometryData) + 1f;
				break;
			}
		}
		float2 yz = 2f;
		int num12 = 0;
		if (!ignoreDriveways && num12 < dynamicBuffer2.Length)
		{
			LaneOverlap laneOverlap3 = dynamicBuffer2[num12++];
			yz = new float2((int)laneOverlap3.m_ThisStart, (int)laneOverlap3.m_ThisEnd) * 0.003921569f;
		}
		while (true)
		{
			if (num10 != 2f || yz.x != 2f)
			{
				float x3;
				if (ignoreDriveways || num10 <= yz.x)
				{
					float7.yz = num10;
					float8.y = float9.x;
					x3 = float9.y;
					num10 = 2f;
					while (num11 < dynamicBuffer.Length)
					{
						LaneObject laneObject4 = dynamicBuffer[num11++];
						if (parkedCarData.HasComponent(laneObject4.m_LaneObject) && !unspawnedData.HasComponent(laneObject4.m_LaneObject))
						{
							num10 = laneObject4.m_CurvePosition.x;
							float9 = GetParkingOffsets(laneObject4.m_LaneObject, ref prefabRefData, ref prefabObjectGeometryData) + 1f;
							break;
						}
					}
				}
				else
				{
					float7.yz = yz;
					float8.y = 0.5f;
					x3 = 0.5f;
					yz = 2f;
					while (num12 < dynamicBuffer2.Length)
					{
						LaneOverlap laneOverlap4 = dynamicBuffer2[num12++];
						float2 float10 = new float2((int)laneOverlap4.m_ThisStart, (int)laneOverlap4.m_ThisEnd) * 0.003921569f;
						if (float10.x <= float7.z)
						{
							float7.z = math.max(float7.z, float10.y);
							continue;
						}
						yz = float10;
						break;
					}
				}
				float3 y = MathUtils.Position(curve.m_Bezier, float7.y);
				if (math.distance(x2, y) - math.csum(float8) >= parkingLength)
				{
					if (curvePos > float7.x && curvePos < float7.y)
					{
						num9++;
						float5 = float7.xy;
						float6 = float8;
						break;
					}
					if (float7.y > minT)
					{
						num9++;
						if (random.NextInt(num9) == 0)
						{
							float5 = float7.xy;
							float6 = float8;
						}
					}
				}
				float7.x = float7.z;
				float8.x = x3;
				x2 = MathUtils.Position(curve.m_Bezier, float7.z);
				continue;
			}
			float7.y = 1f;
			float8.y = math.select(0f, 0.5f, (parkingLane.m_Flags & ParkingLaneFlags.EndingLane) == 0);
			if (!(math.distance(x2, curve.m_Bezier.d) - math.csum(float8) >= parkingLength))
			{
				break;
			}
			if (curvePos > float7.x && curvePos < float7.y)
			{
				num9++;
				float5 = float7.xy;
				float6 = float8;
			}
			else if (float7.y > minT)
			{
				num9++;
				if (random.NextInt(num9) == 0)
				{
					float5 = float7.xy;
					float6 = float8;
				}
			}
			break;
		}
		if (num9 != 0)
		{
			float6 += parkingLength * 0.5f;
			float6.x += parkingOffset;
			float6.y -= parkingOffset;
			Bounds1 t3 = new Bounds1(float5.x, float5.y);
			Bounds1 t4 = new Bounds1(float5.x, float5.y);
			MathUtils.ClampLength(curve.m_Bezier, ref t3, float6.x);
			MathUtils.ClampLengthInverse(curve.m_Bezier, ref t4, float6.y);
			if (curvePos < t3.max || curvePos > t4.min)
			{
				t3.max = math.min(math.max(t3.max, minT), t4.min);
				if (t3.max < t4.min)
				{
					curvePos = random.NextFloat(t3.max, t4.min);
				}
				else
				{
					curvePos = math.lerp(t3.max, t4.min, 0.5f);
				}
			}
			return true;
		}
		return false;
	}

	public static float GetLaneOffset(ObjectGeometryData prefabObjectGeometryData, NetLaneData prefabLaneData, NodeLane nodeLane, float curvePosition, float lanePosition, bool isBicycle)
	{
		float num = prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x;
		num = math.select(num, num * 0.5f, isBicycle);
		float num2 = prefabLaneData.m_Width + math.lerp(nodeLane.m_WidthOffset.x, nodeLane.m_WidthOffset.y, curvePosition);
		float num3 = math.max(0f, num2 - num);
		return lanePosition * num3;
	}

	public static float3 GetLanePosition(Bezier4x3 curve, float curvePosition, float laneOffset)
	{
		float3 result = MathUtils.Position(curve, curvePosition);
		float2 forward = math.normalizesafe(MathUtils.Tangent(curve, curvePosition).xz);
		result.xz += MathUtils.Right(forward) * laneOffset;
		return result;
	}

	public static float3 GetConnectionParkingPosition(Game.Net.ConnectionLane connectionLane, Bezier4x3 curve, float curvePosition)
	{
		float3 @float = math.frac(curvePosition * new float3(100f, 10000f, 1000000f));
		if ((connectionLane.m_Flags & ConnectionLaneFlags.Outside) != 0)
		{
			@float.z -= 0.5f;
			@float *= new float3(40f, 10f, 50f);
		}
		else
		{
			@float.xz -= 0.5f;
			@float *= new float3(25f, 10f, 25f);
		}
		float3 result = MathUtils.Position(curve, curvePosition);
		float2 float2 = math.sign(result.xz);
		float2 float3 = math.abs(result.xz);
		float2 float4 = math.select(new float2(float2.x, 0f), new float2(0f, float2.y), float3.y > float3.x);
		float2 float5 = MathUtils.Right(float4);
		result.xz += float4 * @float.x + float5 * @float.z;
		result.y += @float.y;
		return result;
	}

	public static Bounds3 GetConnectionParkingBounds(Game.Net.ConnectionLane connectionLane, Bezier4x3 curve)
	{
		Bounds3 result = MathUtils.Bounds(curve);
		float3 @float = math.select(new float3(25f, 10f, 25f), new float3(80f, 10f, 80f), (connectionLane.m_Flags & ConnectionLaneFlags.Outside) != 0);
		@float.xz *= 0.5f;
		result.min.xz -= @float.xz;
		result.max += @float;
		return result;
	}

	public static void CheckUnspawned(int jobIndex, Entity entity, CarCurrentLane currentLane, bool isUnspawned, EntityCommandBuffer.ParallelWriter commandBuffer)
	{
		if ((currentLane.m_LaneFlags & (CarLaneFlags.Connection | CarLaneFlags.ResetSpeed)) != 0)
		{
			if (!isUnspawned)
			{
				commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
				commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
			}
		}
		else if ((currentLane.m_LaneFlags & (CarLaneFlags.TransformTarget | CarLaneFlags.ParkingSpace)) == 0 && isUnspawned)
		{
			commandBuffer.RemoveComponent<Unspawned>(jobIndex, entity);
			commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
		}
	}

	public static void CheckUnspawned(int jobIndex, Entity entity, TrainCurrentLane currentLane, bool isUnspawned, EntityCommandBuffer.ParallelWriter commandBuffer)
	{
		if ((currentLane.m_Front.m_LaneFlags & (TrainLaneFlags.ResetSpeed | TrainLaneFlags.Connection)) != 0)
		{
			if (!isUnspawned)
			{
				commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
				commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
			}
		}
		else if (isUnspawned)
		{
			commandBuffer.RemoveComponent<Unspawned>(jobIndex, entity);
			commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
		}
	}

	public static void CheckUnspawned(int jobIndex, Entity entity, AircraftCurrentLane currentLane, bool isUnspawned, EntityCommandBuffer.ParallelWriter commandBuffer)
	{
		if ((currentLane.m_LaneFlags & (AircraftLaneFlags.Connection | AircraftLaneFlags.ResetSpeed)) != 0)
		{
			if (!isUnspawned)
			{
				commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
				commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
			}
		}
		else if ((currentLane.m_LaneFlags & AircraftLaneFlags.TransformTarget) == 0 && isUnspawned)
		{
			commandBuffer.RemoveComponent<Unspawned>(jobIndex, entity);
			commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
		}
	}

	public static void CheckUnspawned(int jobIndex, Entity entity, WatercraftCurrentLane currentLane, bool isUnspawned, EntityCommandBuffer.ParallelWriter commandBuffer)
	{
		if ((currentLane.m_LaneFlags & (WatercraftLaneFlags.ResetSpeed | WatercraftLaneFlags.Connection)) != 0)
		{
			if (!isUnspawned)
			{
				commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
				commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
			}
		}
		else if ((currentLane.m_LaneFlags & WatercraftLaneFlags.TransformTarget) == 0 && isUnspawned)
		{
			commandBuffer.RemoveComponent<Unspawned>(jobIndex, entity);
			commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
		}
	}

	public static bool GetPathElement(int elementIndex, DynamicBuffer<CarNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, out PathElement pathElement)
	{
		if (elementIndex < navigationLanes.Length)
		{
			CarNavigationLane carNavigationLane = navigationLanes[elementIndex];
			pathElement = new PathElement(carNavigationLane.m_Lane, carNavigationLane.m_CurvePosition);
			return true;
		}
		elementIndex -= navigationLanes.Length;
		if (elementIndex < pathElements.Length)
		{
			pathElement = pathElements[elementIndex];
			return true;
		}
		pathElement = default(PathElement);
		return false;
	}

	public static bool GetPathElement(int elementIndex, DynamicBuffer<WatercraftNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, out PathElement pathElement)
	{
		if (elementIndex < navigationLanes.Length)
		{
			WatercraftNavigationLane watercraftNavigationLane = navigationLanes[elementIndex];
			pathElement = new PathElement(watercraftNavigationLane.m_Lane, watercraftNavigationLane.m_CurvePosition);
			return true;
		}
		elementIndex -= navigationLanes.Length;
		if (elementIndex < pathElements.Length)
		{
			pathElement = pathElements[elementIndex];
			return true;
		}
		pathElement = default(PathElement);
		return false;
	}

	public static bool SetTriangleTarget(float3 left, float3 right, float3 next, float3 comparePosition, int elementIndex, DynamicBuffer<CarNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, ref float3 targetPosition, float minDistance, float lanePosition, float curveDelta, float navigationSize, bool isSingle, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves)
	{
		targetPosition = CalculateTriangleTarget(left, right, next, targetPosition, elementIndex, navigationLanes, pathElements, lanePosition, curveDelta, navigationSize, isSingle, transforms, areaLanes, curves);
		return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
	}

	public static bool SetTriangleTarget(float3 left, float3 right, float3 next, float3 comparePosition, int elementIndex, DynamicBuffer<WatercraftNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, ref float3 targetPosition, float minDistance, float lanePosition, float curveDelta, float navigationSize, bool isSingle, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves)
	{
		targetPosition = CalculateTriangleTarget(left, right, next, targetPosition, elementIndex, navigationLanes, pathElements, lanePosition, curveDelta, navigationSize, isSingle, transforms, areaLanes, curves);
		return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
	}

	public static bool SetTriangleTarget(float3 left, float3 right, float3 next, float3 comparePosition, float3 lastTarget, ref float3 targetPosition, float minDistance, float navigationSize, bool isSingle)
	{
		targetPosition = CalculateTriangleTarget(left, right, next, lastTarget, navigationSize, isSingle);
		return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
	}

	private static float3 CalculateTriangleTarget(float3 left, float3 right, float3 next, float3 lastTarget, int elementIndex, DynamicBuffer<CarNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, float lanePosition, float curveDelta, float navigationSize, bool isSingle, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves)
	{
		if (GetPathElement(elementIndex, navigationLanes, pathElements, out var pathElement))
		{
			if (transforms.TryGetComponent(pathElement.m_Target, out var componentData))
			{
				return CalculateTriangleTarget(left, right, next, componentData.m_Position, navigationSize, isSingle);
			}
			if (areaLanes.HasComponent(pathElement.m_Target))
			{
				return CalculateTriangleTarget(left, right, next, lastTarget, navigationSize, isSingle);
			}
			if (curves.TryGetComponent(pathElement.m_Target, out var componentData2))
			{
				float3 target = MathUtils.Position(componentData2.m_Bezier, pathElement.m_TargetDelta.x);
				return CalculateTriangleTarget(left, right, next, target, navigationSize, isSingle);
			}
		}
		return CalculateTriangleTarget(left, right, next, lanePosition, curveDelta, navigationSize, isSingle);
	}

	private static float3 CalculateTriangleTarget(float3 left, float3 right, float3 next, float3 lastTarget, int elementIndex, DynamicBuffer<WatercraftNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, float lanePosition, float curveDelta, float navigationSize, bool isSingle, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves)
	{
		if (GetPathElement(elementIndex, navigationLanes, pathElements, out var pathElement))
		{
			if (transforms.TryGetComponent(pathElement.m_Target, out var componentData))
			{
				return CalculateTriangleTarget(left, right, next, componentData.m_Position, navigationSize, isSingle);
			}
			if (areaLanes.HasComponent(pathElement.m_Target))
			{
				return CalculateTriangleTarget(left, right, next, lastTarget, navigationSize, isSingle);
			}
			if (curves.TryGetComponent(pathElement.m_Target, out var componentData2))
			{
				float3 target = MathUtils.Position(componentData2.m_Bezier, pathElement.m_TargetDelta.x);
				return CalculateTriangleTarget(left, right, next, target, navigationSize, isSingle);
			}
		}
		return CalculateTriangleTarget(left, right, next, lanePosition, curveDelta, navigationSize, isSingle);
	}

	private static float3 CalculateTriangleTarget(float3 left, float3 right, float3 next, float3 target, float navigationSize, bool isSingle)
	{
		float num = navigationSize * 0.5f;
		Triangle3 triangle = new Triangle3(next, left, right);
		if (isSingle)
		{
			float radius;
			float3 @float = MathUtils.Incenter(triangle, out radius);
			MathUtils.Incenter(triangle.xz, out var radius2);
			float num2 = math.saturate(num / radius2);
			triangle.a += (@float - triangle.a) * num2;
			triangle.b += (@float - triangle.b) * num2;
			triangle.c += (@float - triangle.c) * num2;
			if (MathUtils.Distance(triangle.xz, target.xz, out var t) != 0f)
			{
				target = MathUtils.Position(triangle, t);
			}
		}
		else
		{
			float2 float3 = default(float2);
			float2 float2 = default(float2);
			float2.x = MathUtils.Distance(triangle.ba.xz, target.xz, out float3.x);
			float2.y = MathUtils.Distance(triangle.ca.xz, target.xz, out float3.y);
			float2 = ((!MathUtils.Intersect(triangle.xz, target.xz)) ? math.select(new float2(float2.x, 0f - float2.y), new float2(0f - float2.x, float2.y), float2.x > float2.y) : (-float2));
			if (math.any(float2 > 0f - num))
			{
				if (float2.y <= 0f - num)
				{
					float2 float4 = math.normalizesafe(MathUtils.Right(left.xz - next.xz)) * num;
					target = MathUtils.Position(triangle.ba, float3.x);
					target.xz += math.select(float4, -float4, math.dot(float4, right.xz - next.xz) < 0f);
				}
				else if (float2.x <= 0f - num)
				{
					float2 float5 = math.normalizesafe(MathUtils.Left(right.xz - next.xz)) * num;
					target = MathUtils.Position(triangle.ca, float3.y);
					target.xz += math.select(float5, -float5, math.dot(float5, left.xz - next.xz) < 0f);
				}
				else
				{
					target = math.lerp(MathUtils.Position(triangle.ba, float3.x), MathUtils.Position(triangle.ca, float3.y), 0.5f);
				}
			}
		}
		return target;
	}

	private static float3 CalculateTriangleTarget(float3 left, float3 right, float3 next, float lanePosition, float curveDelta, float navigationSize, bool isSingle)
	{
		float num = navigationSize * 0.5f;
		Line3.Segment line = new Line3.Segment(left, right);
		float num2 = lanePosition * math.saturate(1f - navigationSize / MathUtils.Length(line.xz));
		line.a = MathUtils.Position(line, num2 + 0.5f);
		line.b = next;
		float t;
		if (isSingle)
		{
			t = (math.sqrt(math.saturate(1f - curveDelta)) - 0.5f) * math.saturate(1f - navigationSize / MathUtils.Length(line.xz)) + 0.5f;
		}
		else
		{
			float num3 = curveDelta * 2f;
			num3 = math.select(1f - num3, num3 - 1f, curveDelta > 0.5f);
			t = math.sqrt(math.saturate(1f - num3)) * math.saturate(1f - num / MathUtils.Length(line.xz));
		}
		return MathUtils.Position(line, t);
	}

	public static bool SetAreaTarget(float3 prev2, float3 prev, float3 left, float3 right, float3 next, Entity areaEntity, DynamicBuffer<Game.Areas.Node> nodes, float3 comparePosition, int elementIndex, DynamicBuffer<CarNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, ref float3 targetPosition, float minDistance, float lanePosition, float curveDelta, float navigationSize, bool isBackward, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves, ComponentLookup<Owner> owners)
	{
		float num = navigationSize * 0.5f;
		Line3.Segment segment = new Line3.Segment(left, right);
		float num2 = 1f / MathUtils.Length(segment.xz);
		Bounds1 bounds = new Bounds1(math.min(0.5f, num * num2), math.max(0.5f, 1f - num * num2));
		int num3 = elementIndex;
		PathElement pathElement;
		Owner componentData;
		while (GetPathElement(elementIndex, navigationLanes, pathElements, out pathElement) && owners.TryGetComponent(pathElement.m_Target, out componentData) && componentData.m_Owner == areaEntity)
		{
			AreaLane areaLane = areaLanes[pathElement.m_Target];
			bool4 @bool = new bool4(pathElement.m_TargetDelta < 0.5f, pathElement.m_TargetDelta > 0.5f);
			if (math.any(@bool.xy & @bool.wz))
			{
				Line3.Segment segment2 = new Line3.Segment(comparePosition, nodes[areaLane.m_Nodes.y].m_Position);
				Line3.Segment segment3 = new Line3.Segment(comparePosition, nodes[areaLane.m_Nodes.z].m_Position);
				Bounds1 bounds2 = bounds;
				Bounds1 bounds3 = bounds;
				if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment2.xz, out float2 t))
				{
					float num4 = math.max(math.max(0f, 0.4f * math.min(t.y, 1f - t.y) * MathUtils.Length(segment2.xz) * num2), math.max(t.x - bounds.max, bounds.min - t.x));
					if (num4 < bounds.max - bounds.min)
					{
						bounds2 = new Bounds1(math.max(bounds.min, math.min(bounds.max, t.x) - num4), math.min(bounds.max, math.max(bounds.min, t.x) + num4));
					}
				}
				if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment3.xz, out t))
				{
					float num5 = math.max(math.max(0f, 0.4f * math.min(t.y, 1f - t.y) * MathUtils.Length(segment2.xz) * num2), math.max(t.x - bounds.max, bounds.min - t.x));
					if (num5 < bounds.max - bounds.min)
					{
						bounds3 = new Bounds1(math.max(bounds.min, math.min(bounds.max, t.x) - num5), math.min(bounds.max, math.max(bounds.min, t.x) + num5));
					}
				}
				if (!(bounds2.Equals(bounds) & bounds3.Equals(bounds)))
				{
					bounds = bounds2 | bounds3;
					elementIndex++;
					continue;
				}
				elementIndex = navigationLanes.Length + pathElements.Length;
			}
			elementIndex++;
			break;
		}
		if (elementIndex - 1 < navigationLanes.Length + pathElements.Length)
		{
			float3 b;
			if (elementIndex > num3)
			{
				GetPathElement(elementIndex - 1, navigationLanes, pathElements, out var pathElement2);
				AreaLane areaLane2 = areaLanes[pathElement2.m_Target];
				bool test = pathElement2.m_TargetDelta.y > 0.5f;
				b = CalculateTriangleTarget(nodes[areaLane2.m_Nodes.y].m_Position, nodes[areaLane2.m_Nodes.z].m_Position, nodes[math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, test)].m_Position, lanePosition: math.select(lanePosition, 0f - lanePosition, pathElement2.m_TargetDelta.y < pathElement2.m_TargetDelta.x != isBackward), lastTarget: targetPosition, elementIndex: elementIndex, navigationLanes: navigationLanes, pathElements: pathElements, curveDelta: pathElement2.m_TargetDelta.y, navigationSize: navigationSize, isSingle: false, transforms: transforms, areaLanes: areaLanes, curves: curves);
			}
			else
			{
				b = CalculateTriangleTarget(left, right, next, targetPosition, elementIndex, navigationLanes, pathElements, lanePosition, curveDelta, navigationSize, isSingle: false, transforms, areaLanes, curves);
			}
			Line3.Segment segment4 = new Line3.Segment(comparePosition, b);
			if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment4.xz, out float2 t2))
			{
				float num6 = math.max(math.max(0f, 0.4f * math.min(t2.y, 1f - t2.y) * MathUtils.Length(segment4.xz) * num2), math.max(t2.x - bounds.max, bounds.min - t2.x));
				if (num6 < bounds.max - bounds.min)
				{
					bounds = new Bounds1(math.max(bounds.min, math.min(bounds.max, t2.x) - num6), math.min(bounds.max, math.max(bounds.min, t2.x) + num6));
				}
			}
		}
		float lanePosition2 = math.lerp(bounds.min, bounds.max, lanePosition + 0.5f);
		targetPosition = CalculateAreaTarget(prev2, prev, left, right, comparePosition, minDistance, lanePosition2, navigationSize, out var farEnough);
		if (!farEnough)
		{
			return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
		}
		return true;
	}

	public static bool SetAreaTarget(float3 prev2, float3 prev, float3 left, float3 right, float3 next, Entity areaEntity, DynamicBuffer<Game.Areas.Node> nodes, float3 comparePosition, int elementIndex, DynamicBuffer<WatercraftNavigationLane> navigationLanes, NativeArray<PathElement> pathElements, ref float3 targetPosition, float minDistance, float lanePosition, float curveDelta, float navigationSize, bool isBackward, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves, ComponentLookup<Owner> owners)
	{
		float num = navigationSize * 0.5f;
		Line3.Segment segment = new Line3.Segment(left, right);
		float num2 = 1f / MathUtils.Length(segment.xz);
		Bounds1 bounds = new Bounds1(math.min(0.5f, num * num2), math.max(0.5f, 1f - num * num2));
		int num3 = elementIndex;
		PathElement pathElement;
		Owner componentData;
		while (GetPathElement(elementIndex, navigationLanes, pathElements, out pathElement) && owners.TryGetComponent(pathElement.m_Target, out componentData) && componentData.m_Owner == areaEntity)
		{
			AreaLane areaLane = areaLanes[pathElement.m_Target];
			bool4 @bool = new bool4(pathElement.m_TargetDelta < 0.5f, pathElement.m_TargetDelta > 0.5f);
			if (math.any(@bool.xy & @bool.wz))
			{
				Line3.Segment segment2 = new Line3.Segment(comparePosition, nodes[areaLane.m_Nodes.y].m_Position);
				Line3.Segment segment3 = new Line3.Segment(comparePosition, nodes[areaLane.m_Nodes.z].m_Position);
				Bounds1 bounds2 = bounds;
				Bounds1 bounds3 = bounds;
				if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment2.xz, out float2 t))
				{
					float num4 = math.max(math.max(0f, 0.4f * math.min(t.y, 1f - t.y) * MathUtils.Length(segment2.xz) * num2), math.max(t.x - bounds.max, bounds.min - t.x));
					if (num4 < bounds.max - bounds.min)
					{
						bounds2 = new Bounds1(math.max(bounds.min, math.min(bounds.max, t.x) - num4), math.min(bounds.max, math.max(bounds.min, t.x) + num4));
					}
				}
				if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment3.xz, out t))
				{
					float num5 = math.max(math.max(0f, 0.4f * math.min(t.y, 1f - t.y) * MathUtils.Length(segment2.xz) * num2), math.max(t.x - bounds.max, bounds.min - t.x));
					if (num5 < bounds.max - bounds.min)
					{
						bounds3 = new Bounds1(math.max(bounds.min, math.min(bounds.max, t.x) - num5), math.min(bounds.max, math.max(bounds.min, t.x) + num5));
					}
				}
				if (!(bounds2.Equals(bounds) & bounds3.Equals(bounds)))
				{
					bounds = bounds2 | bounds3;
					elementIndex++;
					continue;
				}
				elementIndex = navigationLanes.Length + pathElements.Length;
			}
			elementIndex++;
			break;
		}
		if (elementIndex - 1 < navigationLanes.Length + pathElements.Length)
		{
			float3 b;
			if (elementIndex > num3)
			{
				GetPathElement(elementIndex - 1, navigationLanes, pathElements, out var pathElement2);
				AreaLane areaLane2 = areaLanes[pathElement2.m_Target];
				bool test = pathElement2.m_TargetDelta.y > 0.5f;
				b = CalculateTriangleTarget(nodes[areaLane2.m_Nodes.y].m_Position, nodes[areaLane2.m_Nodes.z].m_Position, nodes[math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, test)].m_Position, lanePosition: math.select(lanePosition, 0f - lanePosition, pathElement2.m_TargetDelta.y < pathElement2.m_TargetDelta.x != isBackward), lastTarget: targetPosition, elementIndex: elementIndex, navigationLanes: navigationLanes, pathElements: pathElements, curveDelta: pathElement2.m_TargetDelta.y, navigationSize: navigationSize, isSingle: false, transforms: transforms, areaLanes: areaLanes, curves: curves);
			}
			else
			{
				b = CalculateTriangleTarget(left, right, next, targetPosition, elementIndex, navigationLanes, pathElements, lanePosition, curveDelta, navigationSize, isSingle: false, transforms, areaLanes, curves);
			}
			Line3.Segment segment4 = new Line3.Segment(comparePosition, b);
			if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment4.xz, out float2 t2))
			{
				float num6 = math.max(math.max(0f, 0.4f * math.min(t2.y, 1f - t2.y) * MathUtils.Length(segment4.xz) * num2), math.max(t2.x - bounds.max, bounds.min - t2.x));
				if (num6 < bounds.max - bounds.min)
				{
					bounds = new Bounds1(math.max(bounds.min, math.min(bounds.max, t2.x) - num6), math.min(bounds.max, math.max(bounds.min, t2.x) + num6));
				}
			}
		}
		float lanePosition2 = math.lerp(bounds.min, bounds.max, lanePosition + 0.5f);
		targetPosition = CalculateAreaTarget(prev2, prev, left, right, comparePosition, minDistance, lanePosition2, navigationSize, out var farEnough);
		if (!farEnough)
		{
			return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
		}
		return true;
	}

	private static float3 CalculateAreaTarget(float3 prev2, float3 prev, float3 left, float3 right, float3 comparePosition, float minDistance, float lanePosition, float navigationSize, out bool farEnough)
	{
		float num = navigationSize * 0.5f;
		Line3.Segment line = new Line3.Segment(left, right);
		line.a = MathUtils.Position(line, lanePosition);
		if (!prev2.Equals(prev))
		{
			Line3.Segment segment = new Line3.Segment(prev2, prev);
			line.b = comparePosition;
			if (MathUtils.Intersect(line.xz, segment.xz, out var t) && math.min(t.y, 1f - t.y) >= num / MathUtils.Length(segment.xz))
			{
				farEnough = false;
				return line.a;
			}
		}
		Triangle3 triangle = new Triangle3(prev, left, right);
		float2 float2 = default(float2);
		float2 @float = default(float2);
		@float.x = MathUtils.Distance(triangle.ba.xz, comparePosition.xz, out float2.x);
		@float.y = MathUtils.Distance(triangle.ca.xz, comparePosition.xz, out float2.y);
		@float = ((!MathUtils.Intersect(triangle.xz, comparePosition.xz)) ? math.select(new float2(@float.x, 0f - @float.y), new float2(0f - @float.x, @float.y), @float.x > @float.y) : (-@float));
		if (math.all(@float <= 0f - num))
		{
			farEnough = false;
			return line.a;
		}
		if (@float.y <= 0f - num)
		{
			float2 float3 = math.normalizesafe(MathUtils.Right(left.xz - prev.xz)) * num;
			line.b = MathUtils.Position(triangle.ba, float2.x);
			line.b.xz += math.select(float3, -float3, math.dot(float3, right.xz - prev.xz) < 0f);
		}
		else if (@float.x <= 0f - num)
		{
			float2 float4 = math.normalizesafe(MathUtils.Left(right.xz - prev.xz)) * num;
			line.b = MathUtils.Position(triangle.ca, float2.y);
			line.b.xz += math.select(float4, -float4, math.dot(float4, left.xz - prev.xz) < 0f);
		}
		else
		{
			line.b = prev;
		}
		float t2;
		float num2 = MathUtils.Distance(line.xz, comparePosition.xz, out t2);
		t2 -= math.sqrt(math.max(0f, minDistance * minDistance - num2 * num2) / MathUtils.LengthSquared(line.xz));
		if (t2 >= 0f)
		{
			farEnough = true;
			return MathUtils.Position(line, t2);
		}
		farEnough = false;
		return line.a;
	}

	public static void ClearNavigationForPathfind(Moving moving, CarData prefabCarData, bool isBicycle, ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes, ref ComponentLookup<Game.Net.CarLane> carLaneLookup, ref ComponentLookup<Game.Net.PedestrianLane> pedestrianLaneLookup, ref ComponentLookup<Curve> curveLookup)
	{
		float num = 4f / 15f;
		float num2 = 1.0666667f + num;
		float num3 = math.min(math.length(moving.m_Velocity), prefabCarData.m_MaxSpeed);
		if (carLaneLookup.HasComponent(currentLane.m_Lane) || (isBicycle && pedestrianLaneLookup.HasComponent(currentLane.m_Lane)))
		{
			Curve curve = curveLookup[currentLane.m_Lane];
			bool flag = currentLane.m_CurvePosition.z < currentLane.m_CurvePosition.x;
			float num4 = math.max(0f, GetBrakingDistance(prefabCarData, num3, num) + num3 * num2 - 0.01f);
			float num5 = num4 / math.max(1E-06f, curve.m_Length) + 1E-06f;
			float num6 = currentLane.m_CurvePosition.x + math.select(num5, 0f - num5, flag);
			currentLane.m_LaneFlags |= CarLaneFlags.ClearedForPathfind;
			if (flag ? (currentLane.m_CurvePosition.z <= num6) : (num6 <= currentLane.m_CurvePosition.z))
			{
				currentLane.m_CurvePosition.z = num6;
				navigationLanes.Clear();
				return;
			}
			num4 -= curve.m_Length * math.abs(currentLane.m_CurvePosition.z - currentLane.m_CurvePosition.x);
			int num7 = 0;
			while (num7 < navigationLanes.Length && num4 > 0f)
			{
				ref CarNavigationLane reference = ref navigationLanes.ElementAt(num7);
				if (!carLaneLookup.HasComponent(reference.m_Lane) && (!isBicycle || !pedestrianLaneLookup.HasComponent(reference.m_Lane)))
				{
					break;
				}
				curve = curveLookup[reference.m_Lane];
				flag = reference.m_CurvePosition.y < reference.m_CurvePosition.x;
				num5 = num4 / math.max(1E-06f, curve.m_Length);
				num6 = reference.m_CurvePosition.x + math.select(num5, 0f - num5, flag);
				reference.m_Flags |= CarLaneFlags.ClearedForPathfind;
				num7++;
				if (flag ? (reference.m_CurvePosition.y <= num6) : (num6 <= reference.m_CurvePosition.y))
				{
					reference.m_CurvePosition.y = num6;
					break;
				}
				num4 -= curve.m_Length * math.abs(reference.m_CurvePosition.y - reference.m_CurvePosition.x);
			}
			if (num7 < navigationLanes.Length)
			{
				navigationLanes.RemoveRange(num7, navigationLanes.Length - num7);
			}
		}
		else
		{
			currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
		}
	}

	public static bool CanUseLane(PathMethod methods, RoadTypes roadTypes, CarLaneData carLaneData)
	{
		if ((roadTypes & carLaneData.m_RoadTypes) == 0)
		{
			return false;
		}
		if ((methods & PathMethod.MediumRoad) != 0)
		{
			return (int)carLaneData.m_MaxSize >= 1;
		}
		if ((methods & PathMethod.Road) != 0)
		{
			return (int)carLaneData.m_MaxSize >= 2;
		}
		if ((methods & PathMethod.Offroad) != 0)
		{
			return (int)carLaneData.m_MaxSize >= 3;
		}
		return false;
	}

	public static PathMethod GetPathMethods(Game.Net.CarLane carLane, CarLaneData carLaneData)
	{
		return carLaneData.m_MaxSize switch
		{
			SizeClass.Medium => (carLaneData.m_RoadTypes & (RoadTypes.Car | RoadTypes.Bicycle)) switch
			{
				RoadTypes.Car | RoadTypes.Bicycle => PathMethod.MediumRoad | PathMethod.Bicycle, 
				RoadTypes.Bicycle => PathMethod.Bicycle, 
				_ => PathMethod.MediumRoad, 
			}, 
			SizeClass.Large => (carLaneData.m_RoadTypes & (RoadTypes.Car | RoadTypes.Bicycle)) switch
			{
				RoadTypes.Car | RoadTypes.Bicycle => PathMethod.Road | PathMethod.Bicycle, 
				RoadTypes.Bicycle => PathMethod.Bicycle, 
				_ => PathMethod.Road, 
			}, 
			SizeClass.Undefined => PathMethod.Road | PathMethod.Offroad, 
			_ => ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking), 
		};
	}

	public static PathMethod GetPathMethods(CarData carData)
	{
		return carData.m_SizeClass switch
		{
			SizeClass.Small => PathMethod.Road | PathMethod.MediumRoad, 
			SizeClass.Medium => PathMethod.Road | PathMethod.MediumRoad, 
			SizeClass.Large => PathMethod.Road, 
			_ => ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking), 
		};
	}

	public static PathMethod GetPathMethods(WatercraftData watercraftData)
	{
		return watercraftData.m_SizeClass switch
		{
			SizeClass.Small => PathMethod.Road | PathMethod.MediumRoad, 
			SizeClass.Medium => PathMethod.Road | PathMethod.MediumRoad, 
			SizeClass.Large => PathMethod.Road, 
			_ => ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking), 
		};
	}

	public static PathMethod GetPathMethods(AircraftData aircraftData)
	{
		return aircraftData.m_SizeClass switch
		{
			SizeClass.Small => PathMethod.Road | PathMethod.Flying | PathMethod.MediumRoad, 
			SizeClass.Medium => PathMethod.Road | PathMethod.Flying | PathMethod.MediumRoad, 
			SizeClass.Large => PathMethod.Road | PathMethod.Flying, 
			_ => ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking), 
		};
	}

	public static int GetTransportCompanyAvailableVehicles(Entity vehicleOwnerEntity, ref BufferLookup<Efficiency> efficiencyBufs, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<TransportCompanyData> transportCompanyDatas, ref BufferLookup<InstalledUpgrade> installedUpgradeBufs)
	{
		float efficiency = 1f;
		if (efficiencyBufs.TryGetBuffer(vehicleOwnerEntity, out var bufferData))
		{
			efficiency = Mathf.Min(BuildingUtils.GetEfficiencyExcludingFactor(bufferData, EfficiencyFactor.LackResources), 1f);
		}
		int num = 0;
		if (UpgradeUtils.TryGetCombinedComponent(vehicleOwnerEntity, out var data, ref prefabRefs, ref transportCompanyDatas, ref installedUpgradeBufs))
		{
			num += BuildingUtils.GetVehicleCapacity(efficiency, data.m_MaxTransports);
		}
		return num;
	}

	public static void CheckParkingSubObject(DynamicBuffer<Game.Objects.SubObject> subObjects, ref int laneCount, ref int parkingCapacity, ref int parkedCarCount, ref int parkingFee, ref ComponentLookup<Game.Net.ParkingLane> parkingLanes, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<Curve> curves, ref ComponentLookup<ParkingLaneData> parkingLaneDatas, ref ComponentLookup<ParkedCar> parkedCars, ref ComponentLookup<GarageLane> garageLanes, ref BufferLookup<LaneObject> laneObjectBufs, ref BufferLookup<Game.Net.SubLane> subLaneBufs, ref BufferLookup<Game.Objects.SubObject> subObjectBufs)
	{
		for (int i = 0; i < subObjects.Length; i++)
		{
			Entity subObject = subObjects[i].m_SubObject;
			if (subLaneBufs.TryGetBuffer(subObject, out var bufferData))
			{
				CheckParkingSubLanes(bufferData, ref laneCount, ref parkingCapacity, ref parkedCarCount, ref parkingFee, ref parkingLanes, ref prefabRefs, ref curves, ref parkingLaneDatas, ref parkedCars, ref garageLanes, ref laneObjectBufs);
			}
			if (subObjectBufs.TryGetBuffer(subObject, out var bufferData2))
			{
				CheckParkingSubObject(bufferData2, ref laneCount, ref parkingCapacity, ref parkedCarCount, ref parkingFee, ref parkingLanes, ref prefabRefs, ref curves, ref parkingLaneDatas, ref parkedCars, ref garageLanes, ref laneObjectBufs, ref subLaneBufs, ref subObjectBufs);
			}
		}
	}

	public static void GetParkingData(SystemBase systemBase, Entity entity, ref int laneCount, ref int parkingCapacity, ref int parkedCarCount, ref int parkingFee)
	{
		ComponentLookup<PrefabRef> prefabRefs = systemBase.GetComponentLookup<PrefabRef>(isReadOnly: true);
		ComponentLookup<Game.Net.ParkingLane> parkingLanes = systemBase.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
		ComponentLookup<Curve> curves = systemBase.GetComponentLookup<Curve>(isReadOnly: true);
		ComponentLookup<ParkingLaneData> parkingLaneDatas = systemBase.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
		ComponentLookup<ParkedCar> parkedCars = systemBase.GetComponentLookup<ParkedCar>(isReadOnly: true);
		ComponentLookup<GarageLane> garageLanes = systemBase.GetComponentLookup<GarageLane>(isReadOnly: true);
		BufferLookup<LaneObject> laneObjectBufs = systemBase.GetBufferLookup<LaneObject>(isReadOnly: true);
		BufferLookup<Game.Net.SubLane> subLaneBufs = systemBase.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
		BufferLookup<Game.Net.SubNet> subNetBufs = systemBase.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
		BufferLookup<Game.Objects.SubObject> subObjectBufs = systemBase.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		GetParkingData(entity, ref laneCount, ref parkingCapacity, ref parkedCarCount, ref parkingFee, ref parkingLanes, ref prefabRefs, ref curves, ref parkingLaneDatas, ref parkedCars, ref garageLanes, ref laneObjectBufs, ref subLaneBufs, ref subNetBufs, ref subObjectBufs);
	}

	public static void GetParkingData(Entity entity, ref int laneCount, ref int parkingCapacity, ref int parkedVehicleCount, ref int parkingFee, ref ComponentLookup<Game.Net.ParkingLane> parkingLanes, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<Curve> curves, ref ComponentLookup<ParkingLaneData> parkingLaneDatas, ref ComponentLookup<ParkedCar> parkedCars, ref ComponentLookup<GarageLane> garageLanes, ref BufferLookup<LaneObject> laneObjectBufs, ref BufferLookup<Game.Net.SubLane> subLaneBufs, ref BufferLookup<Game.Net.SubNet> subNetBufs, ref BufferLookup<Game.Objects.SubObject> subObjectBufs)
	{
		if (subLaneBufs.TryGetBuffer(entity, out var bufferData))
		{
			CheckParkingSubLanes(bufferData, ref laneCount, ref parkingCapacity, ref parkedVehicleCount, ref parkingFee, ref parkingLanes, ref prefabRefs, ref curves, ref parkingLaneDatas, ref parkedCars, ref garageLanes, ref laneObjectBufs);
		}
		if (subNetBufs.TryGetBuffer(entity, out var bufferData2))
		{
			for (int i = 0; i < bufferData2.Length; i++)
			{
				Entity subNet = bufferData2[i].m_SubNet;
				if (subLaneBufs.TryGetBuffer(subNet, out var bufferData3))
				{
					CheckParkingSubLanes(bufferData3, ref laneCount, ref parkingCapacity, ref parkedVehicleCount, ref parkingFee, ref parkingLanes, ref prefabRefs, ref curves, ref parkingLaneDatas, ref parkedCars, ref garageLanes, ref laneObjectBufs);
				}
			}
		}
		if (subObjectBufs.TryGetBuffer(entity, out var bufferData4))
		{
			CheckParkingSubObject(bufferData4, ref laneCount, ref parkingCapacity, ref parkedVehicleCount, ref parkingFee, ref parkingLanes, ref prefabRefs, ref curves, ref parkingLaneDatas, ref parkedCars, ref garageLanes, ref laneObjectBufs, ref subLaneBufs, ref subObjectBufs);
		}
	}

	public static void CheckParkingSubLanes(DynamicBuffer<Game.Net.SubLane> subLaneBuf, ref int laneCount, ref int parkingCapacity, ref int parkedCarCount, ref int parkingFee, ref ComponentLookup<Game.Net.ParkingLane> parkingLanes, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<Curve> curves, ref ComponentLookup<ParkingLaneData> parkingLaneDatas, ref ComponentLookup<ParkedCar> parkedCars, ref ComponentLookup<GarageLane> garageLanes, ref BufferLookup<LaneObject> laneObjectBufs)
	{
		for (int i = 0; i < subLaneBuf.Length; i++)
		{
			Entity subLane = subLaneBuf[i].m_SubLane;
			GarageLane componentData2;
			if (parkingLanes.TryGetComponent(subLane, out var componentData))
			{
				if ((componentData.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
				{
					continue;
				}
				Entity prefab = prefabRefs[subLane].m_Prefab;
				Curve curve = curves[subLane];
				DynamicBuffer<LaneObject> dynamicBuffer = laneObjectBufs[subLane];
				ParkingLaneData prefabParkingLane = parkingLaneDatas[prefab];
				if (prefabParkingLane.m_SlotInterval != 0f)
				{
					int parkingSlotCount = NetUtils.GetParkingSlotCount(curve, componentData, prefabParkingLane);
					parkingCapacity += parkingSlotCount;
				}
				else
				{
					parkingCapacity = -1000000;
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (parkedCars.HasComponent(dynamicBuffer[j].m_LaneObject))
					{
						parkedCarCount++;
					}
				}
				parkingFee += componentData.m_ParkingFee;
				laneCount++;
			}
			else if (garageLanes.TryGetComponent(subLane, out componentData2))
			{
				parkingCapacity += componentData2.m_VehicleCapacity;
				parkedCarCount += componentData2.m_VehicleCount;
				parkingFee += componentData2.m_ParkingFee;
				laneCount++;
			}
		}
	}
}
