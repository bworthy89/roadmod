using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Creatures;

public static class CreatureUtils
{
	private struct ActivityLocationIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_Ignore;

		public Line3.Segment m_Line;

		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		public bool m_Found;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			float2 t;
			if (!m_Found)
			{
				return MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds, 0.5f), m_Line, out t);
			}
			return false;
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
		{
			if (!m_Found && MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds, 0.5f), m_Line, out var _) && !(entity == m_Ignore) && m_TransformData.HasComponent(entity))
			{
				Game.Objects.Transform transform = m_TransformData[entity];
				m_Found |= MathUtils.Distance(m_Line, transform.m_Position, out var _) < 0.5f;
			}
		}
	}

	public const float MAX_HUMAN_WALK_SPEED = 5.555556f;

	public const float AVG_HUMAN_WALK_SPEED = 1.6666667f;

	public const float MIN_MOVE_SPEED = 0.1f;

	public const float RESIDENT_PATHFIND_RANDOM_COST = 30f;

	public const int MAX_TRANSPORT_WAIT_TICKS = 5000;

	public const int MAX_ENTER_VEHICLE_TICKS = 250;

	public const float QUEUE_TICKS_TO_SECONDS = 2f / 15f;

	public static bool PathfindFailed(PathOwner pathOwner)
	{
		return (pathOwner.m_State & (PathFlags.Failed | PathFlags.Stuck)) != 0;
	}

	public static bool EndReached(HumanCurrentLane currentLane)
	{
		return (currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0;
	}

	public static bool PathEndReached(HumanCurrentLane currentLane)
	{
		return (currentLane.m_Flags & (CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached)) == (CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached);
	}

	public static bool PathEndReached(AnimalCurrentLane currentLane)
	{
		return (currentLane.m_Flags & (CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached)) == (CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached);
	}

	public static bool ParkingSpaceReached(HumanCurrentLane currentLane)
	{
		return (currentLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.ParkingSpace)) == (CreatureLaneFlags.EndReached | CreatureLaneFlags.ParkingSpace);
	}

	public static bool ActionLocationReached(HumanCurrentLane currentLane)
	{
		return (currentLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.Action)) == (CreatureLaneFlags.EndReached | CreatureLaneFlags.Action);
	}

	public static bool TransportStopReached(HumanCurrentLane currentLane)
	{
		return (currentLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.Transport)) == (CreatureLaneFlags.EndReached | CreatureLaneFlags.Transport);
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

	public static bool IsStuck(AnimalCurrentLane currentLane)
	{
		return (currentLane.m_Flags & CreatureLaneFlags.Stuck) != 0;
	}

	public static bool ResetUncheckedLane(ref HumanCurrentLane currentLane)
	{
		bool result = (currentLane.m_Flags & CreatureLaneFlags.Checked) == 0;
		currentLane.m_Flags |= CreatureLaneFlags.Checked;
		return result;
	}

	public static void SetupPathfind(ref HumanCurrentLane currentLane, ref PathOwner pathOwner, NativeQueue<SetupQueueItem>.ParallelWriter queue, SetupQueueItem item)
	{
		if ((pathOwner.m_State & (PathFlags.Obsolete | PathFlags.Divert)) == (PathFlags.Obsolete | PathFlags.Divert))
		{
			pathOwner.m_State |= PathFlags.CachedObsolete;
		}
		pathOwner.m_State &= ~(PathFlags.Failed | PathFlags.Obsolete | PathFlags.DivertObsolete);
		pathOwner.m_State |= PathFlags.Pending;
		currentLane.m_Flags &= ~(CreatureLaneFlags.EndOfPath | CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Transport | CreatureLaneFlags.Taxi | CreatureLaneFlags.Action);
		queue.Enqueue(item);
	}

	public static bool DivertDestination(ref SetupQueueTarget destination, ref PathOwner pathOwner, Divert divert)
	{
		if (divert.m_Purpose == Purpose.None)
		{
			return true;
		}
		if (divert.m_Target != Entity.Null)
		{
			destination.m_Entity = divert.m_Target;
			pathOwner.m_State |= PathFlags.Divert;
			return true;
		}
		switch (divert.m_Purpose)
		{
		case Purpose.SendMail:
			destination.m_Type = SetupTargetType.MailBox;
			pathOwner.m_State |= PathFlags.AddDestination | PathFlags.Divert;
			return true;
		case Purpose.Safety:
		case Purpose.Escape:
			destination.m_Type = SetupTargetType.Safety;
			pathOwner.m_State |= PathFlags.Divert;
			return true;
		case Purpose.Disappear:
			destination.m_Type = SetupTargetType.OutsideConnection;
			pathOwner.m_State |= PathFlags.AddDestination | PathFlags.Divert;
			return true;
		case Purpose.WaitingHome:
		case Purpose.PathFailed:
			return false;
		default:
			return true;
		}
	}

	public static bool ResetUpdatedPath(ref PathOwner pathOwner)
	{
		bool result = (pathOwner.m_State & PathFlags.Updated) != 0;
		pathOwner.m_State &= ~PathFlags.Updated;
		return result;
	}

	public static Game.Objects.Transform GetVehicleDoorPosition(ref Unity.Mathematics.Random random, ActivityType activityType, ActivityCondition conditions, Game.Objects.Transform vehicleTransform, PseudoRandomSeed pseudoRandomSeed, float3 targetPosition, bool isDriver, bool lefthandTraffic, Entity creaturePrefab, Entity vehicle, DynamicBuffer<MeshGroup> meshGroups, ref ComponentLookup<Game.Vehicles.PublicTransport> publicTransports, ref ComponentLookup<Train> trains, ref ComponentLookup<Controller> controllers, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<CarData> prefabCarDatas, ref BufferLookup<ActivityLocationElement> prefabActivityLocations, ref BufferLookup<SubMeshGroup> subMeshGroupBuffers, ref BufferLookup<CharacterElement> characterElementBuffers, ref BufferLookup<SubMesh> subMeshBuffers, ref BufferLookup<AnimationClip> animationClipBuffers, ref BufferLookup<AnimationMotion> animationMotionBuffers, out ActivityMask activityMask, out AnimatedPropID propID)
	{
		PrefabRef prefabRef = prefabRefs[vehicle];
		Game.Objects.Transform result = vehicleTransform;
		activityMask = default(ActivityMask);
		propID = new AnimatedPropID(-1);
		if (prefabActivityLocations.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
		{
			ActivityMask activityMask2 = new ActivityMask(ActivityType.Enter);
			ActivityMask activityMask3 = new ActivityMask(ActivityType.Driving);
			activityMask2.m_Mask |= new ActivityMask(ActivityType.Exit).m_Mask;
			activityMask3.m_Mask |= new ActivityMask(ActivityType.Biking).m_Mask;
			int num = 0;
			int num2 = -1;
			bool a = true;
			bool b = true;
			if (publicTransports.TryGetComponent(vehicle, out var componentData))
			{
				bool flag = false;
				if (controllers.TryGetComponent(vehicle, out var componentData2) && publicTransports.TryGetComponent(componentData2.m_Controller, out var componentData3))
				{
					componentData = componentData3;
					if (trains.TryGetComponent(vehicle, out var componentData4) && trains.TryGetComponent(componentData2.m_Controller, out var componentData5))
					{
						flag = ((componentData4.m_Flags ^ componentData5.m_Flags) & Game.Vehicles.TrainFlags.Reversed) != 0;
					}
				}
				a = (componentData.m_State & PublicTransportFlags.StopRight) == 0;
				b = (componentData.m_State & PublicTransportFlags.StopLeft) == 0;
				if (flag)
				{
					CommonUtils.Swap(ref a, ref b);
				}
			}
			else if (prefabCarDatas.HasComponent(prefabRef.m_Prefab))
			{
				float num3 = float.MinValue;
				for (int i = 0; i < bufferData.Length; i++)
				{
					ActivityLocationElement activityLocationElement = bufferData[i];
					if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask2.m_Mask) == activityMask2.m_Mask)
					{
						bool test = ((activityLocationElement.m_ActivityFlags & ActivityFlags.InvertLefthandTraffic) != 0 && lefthandTraffic) || ((activityLocationElement.m_ActivityFlags & ActivityFlags.InvertRighthandTraffic) != 0 && !lefthandTraffic);
						activityLocationElement.m_Position.x = math.select(activityLocationElement.m_Position.x, 0f - activityLocationElement.m_Position.x, test);
						if ((!(math.abs(activityLocationElement.m_Position.x) >= 0.5f) || activityLocationElement.m_Position.x >= 0f == lefthandTraffic) && activityLocationElement.m_Position.z > num3)
						{
							num2 = i;
							num3 = activityLocationElement.m_Position.z;
						}
					}
				}
			}
			isDriver = isDriver && num2 != -1;
			ObjectUtils.ActivityStartPositionCache cache = default(ObjectUtils.ActivityStartPositionCache);
			for (int j = 0; j < bufferData.Length; j++)
			{
				ActivityLocationElement activityLocationElement2 = bufferData[j];
				ActivityMask activityMask4 = new ActivityMask(activityType);
				activityMask4.m_Mask &= activityLocationElement2.m_ActivityMask.m_Mask;
				if (activityMask4.m_Mask == 0 || isDriver != (j == num2))
				{
					continue;
				}
				bool test2 = ((activityLocationElement2.m_ActivityFlags & ActivityFlags.InvertLefthandTraffic) != 0 && lefthandTraffic) || ((activityLocationElement2.m_ActivityFlags & ActivityFlags.InvertRighthandTraffic) != 0 && !lefthandTraffic);
				activityLocationElement2.m_Position.x = math.select(activityLocationElement2.m_Position.x, 0f - activityLocationElement2.m_Position.x, test2);
				if (!(math.abs(activityLocationElement2.m_Position.x) >= 0.5f) || ((activityLocationElement2.m_Position.x >= 0f) ? b : a))
				{
					if (activityType == ActivityType.Exit && (activityLocationElement2.m_ActivityMask.m_Mask & activityMask2.m_Mask) == activityMask2.m_Mask && (activityLocationElement2.m_ActivityMask.m_Mask & activityMask3.m_Mask) == 0)
					{
						activityLocationElement2.m_Rotation = math.mul(quaternion.RotateY(MathF.PI), activityLocationElement2.m_Rotation);
					}
					Game.Objects.Transform transform = ObjectUtils.LocalToWorld(vehicleTransform, activityLocationElement2.m_Position, activityLocationElement2.m_Rotation);
					if (activityType == ActivityType.Enter && (activityLocationElement2.m_ActivityMask.m_Mask & activityMask3.m_Mask) != 0)
					{
						transform = ObjectUtils.GetActivityStartPosition(creaturePrefab, meshGroups, transform, TransformState.Action, activityType, pseudoRandomSeed, activityLocationElement2.m_PropID, conditions, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, ref animationMotionBuffers, ref cache);
					}
					if (math.distancesq(transform.m_Position, targetPosition) < 0.01f)
					{
						activityMask = activityLocationElement2.m_ActivityMask;
						propID = activityLocationElement2.m_PropID;
						return transform;
					}
					if (random.NextInt(++num) == 0)
					{
						result = transform;
						activityMask = activityLocationElement2.m_ActivityMask;
						propID = activityLocationElement2.m_PropID;
					}
				}
			}
		}
		return result;
	}

	public static ActivityCondition GetConditions(Human human)
	{
		ActivityCondition activityCondition = (ActivityCondition)0u;
		if ((human.m_Flags & HumanFlags.Homeless) != 0)
		{
			activityCondition |= ActivityCondition.Homeless;
		}
		if ((human.m_Flags & HumanFlags.Collapsed) != 0)
		{
			activityCondition |= ActivityCondition.Collapsed;
		}
		if ((human.m_Flags & HumanFlags.Angry) != 0)
		{
			activityCondition |= ActivityCondition.Angry;
		}
		else if ((human.m_Flags & HumanFlags.Waiting) != 0)
		{
			activityCondition |= ActivityCondition.Waiting;
		}
		else if ((human.m_Flags & HumanFlags.Sad) != 0)
		{
			activityCondition |= ActivityCondition.Sad;
		}
		else if ((human.m_Flags & HumanFlags.Happy) != 0)
		{
			activityCondition |= ActivityCondition.Happy;
		}
		return activityCondition;
	}

	public static bool CalculateTransformPosition(Entity creature, Entity creaturePrefab, DynamicBuffer<MeshGroup> meshGroups, ref Unity.Mathematics.Random random, ref Game.Objects.Transform result, ref ActivityType activity, CurrentVehicle currentVehicle, PseudoRandomSeed pseudoRandomSeed, Entity entity, bool leftHandTraffic, ActivityMask activityMask, ActivityCondition conditions, NativeQuadTree<Entity, QuadTreeBoundsXZ> movingObjectSearchTree, ref ComponentLookup<Game.Objects.Transform> transforms, ref ComponentLookup<Position> positions, ref ComponentLookup<Game.Vehicles.PublicTransport> publicTransports, ref ComponentLookup<Train> trains, ref ComponentLookup<Controller> controllers, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<BuildingData> prefabBuildingDatas, ref ComponentLookup<CarData> prefabCarDatas, ref BufferLookup<ActivityLocationElement> prefabActivityLocations, ref BufferLookup<SubMeshGroup> subMeshGroupBuffers, ref BufferLookup<CharacterElement> characterElementBuffers, ref BufferLookup<SubMesh> subMeshBuffers, ref BufferLookup<AnimationClip> animationClipBuffers, ref BufferLookup<AnimationMotion> animationMotionBuffers)
	{
		if (transforms.HasComponent(entity))
		{
			Game.Objects.Transform transform = transforms[entity];
			PrefabRef prefabRef = prefabRefs[entity];
			if (entity == currentVehicle.m_Vehicle)
			{
				float3 position = result.m_Position;
				bool isDriver = (currentVehicle.m_Flags & CreatureVehicleFlags.Driver) != 0;
				result = GetVehicleDoorPosition(ref random, ActivityType.Enter, conditions, transform, pseudoRandomSeed, position, isDriver, leftHandTraffic, creaturePrefab, entity, meshGroups, ref publicTransports, ref trains, ref controllers, ref prefabRefs, ref prefabCarDatas, ref prefabActivityLocations, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, ref animationMotionBuffers, out var activityMask2, out var _);
				ActivityMask activityMask3 = new ActivityMask(ActivityType.Driving);
				activityMask3.m_Mask |= new ActivityMask(ActivityType.Biking).m_Mask;
				if ((activityMask2.m_Mask & activityMask3.m_Mask) != 0)
				{
					activity = ActivityType.Enter;
				}
				return true;
			}
			if (prefabBuildingDatas.HasComponent(prefabRef.m_Prefab))
			{
				BuildingData buildingData = prefabBuildingDatas[prefabRef.m_Prefab];
				result.m_Position = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
				return true;
			}
			if (prefabActivityLocations.HasBuffer(prefabRef.m_Prefab))
			{
				DynamicBuffer<ActivityLocationElement> dynamicBuffer = prefabActivityLocations[prefabRef.m_Prefab];
				float num = float.MaxValue;
				float3 position2 = result.m_Position;
				ActivityLocationIterator iterator = new ActivityLocationIterator
				{
					m_Ignore = creature,
					m_TransformData = transforms
				};
				ObjectUtils.ActivityStartPositionCache cache = default(ObjectUtils.ActivityStartPositionCache);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					ActivityLocationElement activityLocationElement = dynamicBuffer[i];
					ActivityMask activityMask4 = activityMask;
					activityMask4.m_Mask &= activityLocationElement.m_ActivityMask.m_Mask;
					if (activityMask4.m_Mask == 0)
					{
						continue;
					}
					Game.Objects.Transform activityTransform = ObjectUtils.LocalToWorld(transform, activityLocationElement.m_Position, activityLocationElement.m_Rotation);
					float3 @float = math.forward(activityTransform.m_Rotation);
					iterator.m_Line = new Line3.Segment(activityTransform.m_Position, activityTransform.m_Position + @float);
					iterator.m_Found = false;
					movingObjectSearchTree.Iterate(ref iterator);
					if (iterator.m_Found)
					{
						continue;
					}
					int num2 = random.NextInt(math.countbits(activityMask4.m_Mask));
					for (int j = 1; j <= 64; j++)
					{
						ActivityType activityType = (ActivityType)j;
						if ((activityMask4.m_Mask & new ActivityMask(activityType).m_Mask) != 0 && num2-- == 0)
						{
							activity = activityType;
							break;
						}
					}
					ActivityType parentActivity = (ActivityType)ObjectUtils.GetParentActivity((byte)activity);
					parentActivity = ((parentActivity == ActivityType.None) ? activity : parentActivity);
					activityTransform = ObjectUtils.GetActivityStartPosition(creaturePrefab, meshGroups, activityTransform, TransformState.Start, parentActivity, pseudoRandomSeed, activityLocationElement.m_PropID, conditions, ref subMeshGroupBuffers, ref characterElementBuffers, ref subMeshBuffers, ref animationClipBuffers, ref animationMotionBuffers, ref cache);
					float num3 = math.distance(activityTransform.m_Position, position2);
					num3 *= random.NextFloat(0.5f, 1.5f);
					if (!(num3 >= num))
					{
						num = num3;
						result = activityTransform;
					}
				}
				return num != float.MaxValue;
			}
			result.m_Position = transform.m_Position;
			return true;
		}
		if (positions.HasComponent(entity))
		{
			result.m_Position = positions[entity].m_Position;
			return true;
		}
		return false;
	}

	public static void GetAreaActivity(ref Unity.Mathematics.Random random, ref ActivityType activity, Entity laneEntity, ActivityMask activityMask, ComponentLookup<Owner> owners, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<SpawnLocationData> prefabSpawnLocationDatas)
	{
		if (!owners.HasComponent(laneEntity))
		{
			return;
		}
		Entity owner = owners[laneEntity].m_Owner;
		PrefabRef prefabRef = prefabRefs[owner];
		if (!prefabSpawnLocationDatas.HasComponent(prefabRef.m_Prefab))
		{
			return;
		}
		SpawnLocationData spawnLocationData = prefabSpawnLocationDatas[prefabRef.m_Prefab];
		activityMask.m_Mask &= spawnLocationData.m_ActivityMask.m_Mask;
		if (activityMask.m_Mask == 0)
		{
			return;
		}
		int num = random.NextInt(math.countbits(activityMask.m_Mask));
		for (int i = 1; i <= 64; i++)
		{
			ActivityType activityType = (ActivityType)i;
			if ((activityMask.m_Mask & new ActivityMask(activityType).m_Mask) != 0 && num-- == 0)
			{
				activity = activityType;
				break;
			}
		}
	}

	public static void GetLaneActivity(ref Unity.Mathematics.Random random, ref ActivityType activity, Entity lanePrefab, ActivityMask activityMask, ref ComponentLookup<PedestrianLaneData> prefabPedestrianLaneDatas)
	{
		if (!prefabPedestrianLaneDatas.TryGetComponent(lanePrefab, out var componentData))
		{
			return;
		}
		activityMask.m_Mask &= componentData.m_ActivityMask.m_Mask;
		if (activityMask.m_Mask == 0)
		{
			return;
		}
		int num = random.NextInt(math.countbits(activityMask.m_Mask));
		for (int i = 1; i <= 64; i++)
		{
			ActivityType activityType = (ActivityType)i;
			if ((activityMask.m_Mask & new ActivityMask(activityType).m_Mask) != 0 && num-- == 0)
			{
				activity = activityType;
				break;
			}
		}
	}

	public static bool SetTriangleTarget(float3 left, float3 right, float3 next, float3 comparePosition, PathElement nextElement, int elementIndex, DynamicBuffer<PathElement> pathElements, ref float3 targetPosition, float minDistance, float lanePosition, float curveDelta, float navigationSize, bool isSingle, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<TaxiStand> taxiStands, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves)
	{
		targetPosition = CalculateTriangleTarget(left, right, next, targetPosition, nextElement, elementIndex, pathElements, lanePosition, curveDelta, navigationSize, isSingle, transforms, taxiStands, areaLanes, curves);
		return math.distance(comparePosition, targetPosition) >= minDistance;
	}

	private static float3 CalculateTriangleTarget(float3 left, float3 right, float3 next, float3 lastTarget, PathElement nextElement, int elementIndex, DynamicBuffer<PathElement> pathElements, float lanePosition, float curveDelta, float navigationSize, bool isSingle, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<TaxiStand> taxiStands, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves)
	{
		if (nextElement.m_Target == Entity.Null && pathElements.IsCreated && elementIndex < pathElements.Length)
		{
			nextElement = pathElements[elementIndex];
		}
		if (nextElement.m_Target != Entity.Null)
		{
			if (transforms.TryGetComponent(nextElement.m_Target, out var componentData) && !taxiStands.HasComponent(nextElement.m_Target))
			{
				return CalculateTriangleTarget(left, right, next, componentData.m_Position, navigationSize, isSingle);
			}
			if (areaLanes.HasComponent(nextElement.m_Target))
			{
				return CalculateTriangleTarget(left, right, next, lastTarget, navigationSize, isSingle);
			}
			if (curves.TryGetComponent(nextElement.m_Target, out var componentData2))
			{
				float3 target = MathUtils.Position(componentData2.m_Bezier, nextElement.m_TargetDelta.x);
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

	public static bool SetAreaTarget(float3 prev2, float3 prev, float3 left, float3 right, float3 next, Entity areaEntity, DynamicBuffer<Game.Areas.Node> nodes, float3 comparePosition, PathElement nextElement, int elementIndex, DynamicBuffer<PathElement> pathElements, ref float3 targetPosition, float minDistance, float lanePosition, float curveDelta, float navigationSize, bool isBackward, ComponentLookup<Game.Objects.Transform> transforms, ComponentLookup<TaxiStand> taxiStands, ComponentLookup<AreaLane> areaLanes, ComponentLookup<Curve> curves, ComponentLookup<Owner> owners)
	{
		float num = navigationSize * 0.5f;
		Line3.Segment segment = new Line3.Segment(left, right);
		float num2 = 1f / MathUtils.Length(segment.xz);
		Bounds1 bounds = new Bounds1(math.min(0.5f, num * num2), math.max(0.5f, 1f - num * num2));
		int num3 = 0;
		int num4 = elementIndex;
		if (pathElements.IsCreated)
		{
			num3 = pathElements.Length;
			elementIndex = math.min(elementIndex, num3);
		}
		elementIndex -= math.select(0, 1, nextElement.m_Target != Entity.Null);
		int num5 = elementIndex;
		while (elementIndex < num3)
		{
			PathElement pathElement = ((elementIndex >= num4) ? pathElements[elementIndex] : nextElement);
			if (!owners.TryGetComponent(pathElement.m_Target, out var componentData) || !(componentData.m_Owner == areaEntity))
			{
				break;
			}
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
					float num6 = math.max(math.max(0f, 0.4f * math.min(t.y, 1f - t.y) * MathUtils.Length(segment2.xz) * num2), math.max(t.x - bounds.max, bounds.min - t.x));
					if (num6 < bounds.max - bounds.min)
					{
						bounds2 = new Bounds1(math.max(bounds.min, math.min(bounds.max, t.x) - num6), math.min(bounds.max, math.max(bounds.min, t.x) + num6));
					}
				}
				if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment3.xz, out t))
				{
					float num7 = math.max(math.max(0f, 0.4f * math.min(t.y, 1f - t.y) * MathUtils.Length(segment2.xz) * num2), math.max(t.x - bounds.max, bounds.min - t.x));
					if (num7 < bounds.max - bounds.min)
					{
						bounds3 = new Bounds1(math.max(bounds.min, math.min(bounds.max, t.x) - num7), math.min(bounds.max, math.max(bounds.min, t.x) + num7));
					}
				}
				if (!(bounds2.Equals(bounds) & bounds3.Equals(bounds)))
				{
					bounds = bounds2 | bounds3;
					elementIndex++;
					continue;
				}
				elementIndex = num3;
			}
			elementIndex++;
			break;
		}
		if (elementIndex - 1 < num3)
		{
			float3 b;
			if (elementIndex > num5)
			{
				PathElement pathElement2 = ((elementIndex - 1 >= num4) ? pathElements[elementIndex - 1] : nextElement);
				AreaLane areaLane2 = areaLanes[pathElement2.m_Target];
				bool test = pathElement2.m_TargetDelta.y > 0.5f;
				b = CalculateTriangleTarget(nodes[areaLane2.m_Nodes.y].m_Position, nodes[areaLane2.m_Nodes.z].m_Position, nodes[math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, test)].m_Position, lanePosition: math.select(lanePosition, 0f - lanePosition, pathElement2.m_TargetDelta.y < pathElement2.m_TargetDelta.x != isBackward), lastTarget: targetPosition, nextElement: default(PathElement), elementIndex: elementIndex, pathElements: pathElements, curveDelta: pathElement2.m_TargetDelta.y, navigationSize: navigationSize, isSingle: false, transforms: transforms, taxiStands: taxiStands, areaLanes: areaLanes, curves: curves);
			}
			else
			{
				b = CalculateTriangleTarget(left, right, next, targetPosition, nextElement, elementIndex, pathElements, lanePosition, curveDelta, navigationSize, isSingle: false, transforms, taxiStands, areaLanes, curves);
			}
			Line3.Segment segment4 = new Line3.Segment(comparePosition, b);
			if (MathUtils.Intersect((Line2)segment.xz, (Line2)segment4.xz, out float2 t2))
			{
				float num8 = math.max(math.max(0f, 0.4f * math.min(t2.y, 1f - t2.y) * MathUtils.Length(segment4.xz) * num2), math.max(t2.x - bounds.max, bounds.min - t2.x));
				if (num8 < bounds.max - bounds.min)
				{
					bounds = new Bounds1(math.max(bounds.min, math.min(bounds.max, t2.x) - num8), math.min(bounds.max, math.max(bounds.min, t2.x) + num8));
				}
			}
		}
		float lanePosition2 = math.lerp(bounds.min, bounds.max, lanePosition + 0.5f);
		targetPosition = CalculateAreaTarget(prev2, prev, left, right, comparePosition, minDistance, lanePosition2, navigationSize, out var farEnough);
		if (!farEnough)
		{
			return math.distance(comparePosition, targetPosition) >= minDistance;
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
		float num2 = MathUtils.Distance(line, comparePosition, out t2);
		t2 -= math.sqrt(math.max(0f, minDistance * minDistance - num2 * num2) / MathUtils.LengthSquared(line));
		if (t2 >= 0f)
		{
			farEnough = true;
			return MathUtils.Position(line, t2);
		}
		farEnough = false;
		return line.a;
	}

	public static float GetNavigationSize(ObjectGeometryData prefabObjectGeometryData)
	{
		return prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x;
	}

	public static float GetLaneOffset(ObjectGeometryData prefabObjectGeometryData, NetLaneData prefabLaneData, NodeLane nodeLane, float curvePosition, float lanePosition)
	{
		float navigationSize = GetNavigationSize(prefabObjectGeometryData);
		float num = prefabLaneData.m_Width + math.lerp(nodeLane.m_WidthOffset.x, nodeLane.m_WidthOffset.y, curvePosition);
		float num2 = math.max(0f, num - navigationSize);
		return lanePosition * num2;
	}

	public static float3 GetLanePosition(Bezier4x3 curve, float curvePosition, float laneOffset)
	{
		float3 result = MathUtils.Position(curve, curvePosition);
		float2 forward = math.normalizesafe(MathUtils.Tangent(curve, curvePosition).xz);
		result.xz += MathUtils.Right(forward) * laneOffset;
		return result;
	}

	public static float GetMaxBrakingSpeed(HumanData prefabHumanData, float distance, float timeStep)
	{
		float num = timeStep * prefabHumanData.m_Acceleration;
		return math.sqrt(num * num + 2f * prefabHumanData.m_Acceleration * distance) - num;
	}

	public static float GetMaxBrakingSpeed(HumanData prefabHumanData, float distance, float maxResultSpeed, float timeStep)
	{
		float num = timeStep * prefabHumanData.m_Acceleration;
		return math.sqrt(num * num + 2f * prefabHumanData.m_Acceleration * distance + maxResultSpeed * maxResultSpeed) - num;
	}

	public static float GetBrakingDistance(HumanData prefabHumanData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabHumanData.m_Acceleration + speed * timeStep;
	}

	public static float GetMaxBrakingSpeed(AnimalData prefabAnimalData, float distance, float timeStep)
	{
		float num = timeStep * prefabAnimalData.m_Acceleration;
		return math.sqrt(num * num + 2f * prefabAnimalData.m_Acceleration * distance) - num;
	}

	public static float GetMaxBrakingSpeed(AnimalData prefabAnimalData, float distance, float maxResultSpeed, float timeStep)
	{
		float num = timeStep * prefabAnimalData.m_Acceleration;
		return math.sqrt(num * num + 2f * prefabAnimalData.m_Acceleration * distance + maxResultSpeed * maxResultSpeed) - num;
	}

	public static float GetBrakingDistance(AnimalData prefabAnimalData, float speed, float timeStep)
	{
		return 0.5f * speed * speed / prefabAnimalData.m_Acceleration + speed * timeStep;
	}

	public static Sphere3 GetQueueArea(ObjectGeometryData prefabObjectGeometryData, float3 position)
	{
		Sphere3 result = default(Sphere3);
		result.radius = (prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x) * 0.5f + 0.25f;
		result.position = position;
		return result;
	}

	public static Sphere3 GetQueueArea(ObjectGeometryData prefabObjectGeometryData, float3 position1, float3 position2)
	{
		Sphere3 result = default(Sphere3);
		result.radius = (prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x + math.distance(position1, position2)) * 0.5f + 0.25f;
		result.position = math.lerp(position1, position2, 0.5f);
		return result;
	}

	public static void SetQueue(ref Entity queueEntity, ref Sphere3 queueArea, Entity setEntity, Sphere3 setArea)
	{
		if (queueArea.radius > 0f && setArea.radius > 0f && queueEntity == setEntity)
		{
			queueArea = MathUtils.Sphere(queueArea, setArea);
			return;
		}
		queueEntity = setEntity;
		queueArea = setArea;
	}

	public static void FixPathStart(ref Unity.Mathematics.Random random, float3 position, int elementIndex, DynamicBuffer<PathElement> path, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Lane> laneData, ref ComponentLookup<EdgeLane> edgeLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<Curve> curveData, ref BufferLookup<Game.Net.SubLane> subLanes, ref BufferLookup<Game.Areas.Node> areaNodes, ref BufferLookup<Triangle> areaTriangles)
	{
		if (path.Length <= elementIndex)
		{
			return;
		}
		PathElement pathElement = path[elementIndex];
		if (connectionLaneData.TryGetComponent(pathElement.m_Target, out var componentData))
		{
			if ((componentData.m_Flags & ConnectionLaneFlags.Area) != 0)
			{
				FixPathStart_AreaLane(ref random, position, elementIndex, path, ref ownerData, ref curveData, ref laneData, ref connectionLaneData, ref subLanes, ref areaNodes, ref areaTriangles);
			}
		}
		else if (curveData.HasComponent(pathElement.m_Target))
		{
			FixPathStart_EdgeLane(ref random, position, elementIndex, path, ref ownerData, ref laneData, ref edgeLaneData, ref curveData, ref subLanes);
		}
	}

	private static void FixPathStart_AreaLane(ref Unity.Mathematics.Random random, float3 position, int elementIndex, DynamicBuffer<PathElement> path, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Lane> laneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref BufferLookup<Game.Net.SubLane> subLanes, ref BufferLookup<Game.Areas.Node> areaNodes, ref BufferLookup<Triangle> areaTriangles)
	{
		Entity owner = ownerData[path[elementIndex].m_Target].m_Owner;
		DynamicBuffer<Game.Areas.Node> nodes = areaNodes[owner];
		DynamicBuffer<Triangle> dynamicBuffer = areaTriangles[owner];
		int num = -1;
		float num2 = float.MaxValue;
		float2 t = 0f;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			float2 t2;
			float num3 = MathUtils.Distance(AreaUtils.GetTriangle3(nodes, dynamicBuffer[i]), position, out t2) + random.NextFloat(0.5f);
			if (num3 < num2)
			{
				num2 = num3;
				num = i;
				t = t2;
			}
		}
		if (num == -1)
		{
			return;
		}
		DynamicBuffer<Game.Net.SubLane> lanes = subLanes[owner];
		Triangle3 triangle = AreaUtils.GetTriangle3(nodes, dynamicBuffer[num]);
		float3 position2 = MathUtils.Position(triangle, t);
		num2 = float.MaxValue;
		Entity entity = Entity.Null;
		float startCurvePos = 0f;
		for (int j = 0; j < lanes.Length; j++)
		{
			Entity subLane = lanes[j].m_SubLane;
			if (!connectionLaneData.HasComponent(subLane) || (connectionLaneData[subLane].m_Flags & ConnectionLaneFlags.Pedestrian) == 0)
			{
				continue;
			}
			Curve curve = curveData[subLane];
			float2 t3;
			bool2 x = new bool2(MathUtils.Intersect(triangle.xz, curve.m_Bezier.a.xz, out t3), MathUtils.Intersect(triangle.xz, curve.m_Bezier.d.xz, out t3));
			if (math.any(x))
			{
				float t4;
				float num4 = MathUtils.Distance(curve.m_Bezier, position2, out t4);
				if (num4 < num2)
				{
					float2 @float = math.select(new float2(0f, 0.49f), math.select(new float2(0.51f, 1f), new float2(0f, 1f), x.x), x.y);
					num2 = num4;
					entity = subLane;
					startCurvePos = math.clamp(t4, @float.x, @float.y);
				}
			}
		}
		if (entity == Entity.Null)
		{
			UnityEngine.Debug.Log($"Start path lane not found ({position.x}, {position.y}, {position.z})");
			return;
		}
		int k;
		Owner componentData;
		for (k = elementIndex; k < path.Length - 1 && ownerData.TryGetComponent(path[k + 1].m_Target, out componentData); k++)
		{
			if (componentData.m_Owner != owner)
			{
				break;
			}
		}
		NativeList<PathElement> path2 = new NativeList<PathElement>(lanes.Length, Allocator.Temp);
		PathElement pathElement = path[k];
		AreaUtils.FindAreaPath(ref random, path2, lanes, entity, startCurvePos, pathElement.m_Target, pathElement.m_TargetDelta.y, laneData, curveData);
		if (path2.Length != 0)
		{
			int num5 = k - elementIndex + 1;
			int num6 = math.min(num5, path2.Length);
			for (int l = 0; l < num6; l++)
			{
				path[elementIndex + l] = path2[l];
			}
			if (path2.Length < num5)
			{
				path.RemoveRange(elementIndex + path2.Length, num5 - path2.Length);
			}
			else
			{
				for (int m = num5; m < path2.Length; m++)
				{
					path.Insert(elementIndex + m, path2[m]);
				}
			}
		}
		path2.Dispose();
	}

	private static void FixPathStart_EdgeLane(ref Unity.Mathematics.Random random, float3 position, int elementIndex, DynamicBuffer<PathElement> path, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Lane> laneData, ref ComponentLookup<EdgeLane> edgeLaneData, ref ComponentLookup<Curve> curveData, ref BufferLookup<Game.Net.SubLane> subLanes)
	{
		PathElement pathElement = path[elementIndex];
		if (!edgeLaneData.HasComponent(pathElement.m_Target))
		{
			Lane lane = laneData[pathElement.m_Target];
			bool flag = pathElement.m_TargetDelta.x < 0.5f;
			if (!NetUtils.FindEdgeLane(ref pathElement.m_Target, ref ownerData, ref laneData, ref subLanes, flag))
			{
				return;
			}
			pathElement.m_TargetDelta = (flag ? lane.m_StartNode.GetCurvePos() : lane.m_EndNode.GetCurvePos());
			path.Insert(elementIndex, pathElement);
		}
		Curve curve = curveData[pathElement.m_Target];
		Entity entity = pathElement.m_Target;
		float t;
		float num = MathUtils.Distance(curve.m_Bezier, position, out t) + random.NextFloat(0.5f);
		Entity entity2 = pathElement.m_Target;
		if (NetUtils.FindPrevLane(ref entity2, ref ownerData, ref laneData, ref subLanes))
		{
			float t2;
			float num2 = MathUtils.Distance(curveData[entity2].m_Bezier, position, out t2) + random.NextFloat(0.5f);
			if (num2 < num)
			{
				entity = entity2;
				num = num2;
				t = t2;
			}
		}
		Entity entity3 = pathElement.m_Target;
		if (NetUtils.FindNextLane(ref entity3, ref ownerData, ref laneData, ref subLanes))
		{
			float t3;
			float num3 = MathUtils.Distance(curveData[entity3].m_Bezier, position, out t3) + random.NextFloat(0.5f);
			if (num3 < num)
			{
				entity = entity3;
				num = num3;
				t = t3;
			}
		}
		curve = curveData[entity];
		float num4 = random.NextFloat(-0.5f, 0.5f);
		if (num4 >= 0f)
		{
			Bounds1 t4 = new Bounds1(t, 1f);
			t = ((!MathUtils.ClampLength(curve.m_Bezier.xz, ref t4, num4)) ? math.saturate(t + (1f - t) * num4 / 0.5f) : t4.max);
		}
		else
		{
			num4 = 0f - num4;
			Bounds1 t5 = new Bounds1(0f, t);
			t = ((!MathUtils.ClampLengthInverse(curve.m_Bezier.xz, ref t5, num4)) ? math.saturate(t - t * num4 / 0.5f) : t5.min);
		}
		if (entity == pathElement.m_Target)
		{
			pathElement.m_TargetDelta.x = t;
			path[elementIndex] = pathElement;
		}
		else if (entity == entity3)
		{
			if (elementIndex < path.Length - 1 && path[elementIndex + 1].m_Target == entity3)
			{
				path.RemoveAt(elementIndex);
				pathElement = path[elementIndex];
				pathElement.m_TargetDelta.x = t;
				path[elementIndex] = pathElement;
			}
			else
			{
				path.Insert(elementIndex + 1, new PathElement
				{
					m_Target = pathElement.m_Target,
					m_TargetDelta = new float2(1f, pathElement.m_TargetDelta.y)
				});
				pathElement.m_Target = entity3;
				pathElement.m_TargetDelta = new float2(t, 0f);
				path[elementIndex] = pathElement;
			}
		}
		else if (entity == entity2)
		{
			if (elementIndex < path.Length - 1 && path[elementIndex + 1].m_Target == entity2)
			{
				path.RemoveAt(elementIndex);
				pathElement = path[elementIndex];
				pathElement.m_TargetDelta.x = t;
				path[elementIndex] = pathElement;
			}
			else
			{
				path.Insert(elementIndex + 1, new PathElement
				{
					m_Target = pathElement.m_Target,
					m_TargetDelta = new float2(0f, pathElement.m_TargetDelta.y)
				});
				pathElement.m_Target = entity2;
				pathElement.m_TargetDelta = new float2(t, 1f);
				path[elementIndex] = pathElement;
			}
		}
	}

	public static void FixEnterPath(ref Unity.Mathematics.Random random, float3 position, int elementIndex, DynamicBuffer<PathElement> path, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Lane> laneData, ref ComponentLookup<EdgeLane> edgeLaneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref ComponentLookup<Curve> curveData, ref BufferLookup<Game.Net.SubLane> subLanes, ref BufferLookup<Game.Areas.Node> areaNodes, ref BufferLookup<Triangle> areaTriangles)
	{
		if (path.Length <= elementIndex)
		{
			return;
		}
		PathElement pathElement = path[elementIndex];
		if (connectionLaneData.TryGetComponent(pathElement.m_Target, out var componentData))
		{
			if ((componentData.m_Flags & ConnectionLaneFlags.Area) != 0)
			{
				FixEnterPath_AreaLane(ref random, position, elementIndex, path, ref ownerData, ref curveData, ref laneData, ref connectionLaneData, ref subLanes, ref areaNodes, ref areaTriangles);
			}
		}
		else if (curveData.HasComponent(pathElement.m_Target))
		{
			FixEnterPath_EdgeLane(ref random, position, elementIndex, path, ref ownerData, ref laneData, ref edgeLaneData, ref curveData, ref subLanes);
		}
	}

	private static void FixEnterPath_AreaLane(ref Unity.Mathematics.Random random, float3 position, int elementIndex, DynamicBuffer<PathElement> path, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Curve> curveData, ref ComponentLookup<Lane> laneData, ref ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ref BufferLookup<Game.Net.SubLane> subLanes, ref BufferLookup<Game.Areas.Node> areaNodes, ref BufferLookup<Triangle> areaTriangles)
	{
		Entity owner = ownerData[path[elementIndex].m_Target].m_Owner;
		DynamicBuffer<Game.Areas.Node> nodes = areaNodes[owner];
		DynamicBuffer<Triangle> dynamicBuffer = areaTriangles[owner];
		int num = -1;
		float num2 = float.MaxValue;
		float2 t = 0f;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			float2 t2;
			float num3 = MathUtils.Distance(AreaUtils.GetTriangle3(nodes, dynamicBuffer[i]), position, out t2) + random.NextFloat(0.5f);
			if (num3 < num2)
			{
				num2 = num3;
				num = i;
				t = t2;
			}
		}
		if (num == -1)
		{
			return;
		}
		DynamicBuffer<Game.Net.SubLane> lanes = subLanes[owner];
		Triangle3 triangle = AreaUtils.GetTriangle3(nodes, dynamicBuffer[num]);
		float3 position2 = MathUtils.Position(triangle, t);
		num2 = float.MaxValue;
		Entity entity = Entity.Null;
		float endCurvePos = 0f;
		for (int j = 0; j < lanes.Length; j++)
		{
			Entity subLane = lanes[j].m_SubLane;
			if (!connectionLaneData.HasComponent(subLane) || (connectionLaneData[subLane].m_Flags & ConnectionLaneFlags.Pedestrian) == 0)
			{
				continue;
			}
			Curve curve = curveData[subLane];
			float2 t3;
			bool2 x = new bool2(MathUtils.Intersect(triangle.xz, curve.m_Bezier.a.xz, out t3), MathUtils.Intersect(triangle.xz, curve.m_Bezier.d.xz, out t3));
			if (math.any(x))
			{
				float t4;
				float num4 = MathUtils.Distance(curve.m_Bezier, position2, out t4);
				if (num4 < num2)
				{
					float2 @float = math.select(new float2(0f, 0.49f), math.select(new float2(0.51f, 1f), new float2(0f, 1f), x.x), x.y);
					num2 = num4;
					entity = subLane;
					endCurvePos = math.clamp(t4, @float.x, @float.y);
				}
			}
		}
		if (entity == Entity.Null)
		{
			UnityEngine.Debug.Log($"Enter path lane not found ({position.x}, {position.y}, {position.z})");
			return;
		}
		NativeList<PathElement> path2 = new NativeList<PathElement>(lanes.Length, Allocator.Temp);
		PathElement pathElement = path[elementIndex];
		AreaUtils.FindAreaPath(ref random, path2, lanes, pathElement.m_Target, pathElement.m_TargetDelta.x, entity, endCurvePos, laneData, curveData);
		if (path2.Length != 0)
		{
			path[elementIndex] = path2[0];
			for (int k = 1; k < path2.Length; k++)
			{
				path.Insert(elementIndex + k, path2[k]);
			}
		}
		path2.Dispose();
	}

	private static void FixEnterPath_EdgeLane(ref Unity.Mathematics.Random random, float3 position, int elementIndex, DynamicBuffer<PathElement> path, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Lane> laneData, ref ComponentLookup<EdgeLane> edgeLaneData, ref ComponentLookup<Curve> curveData, ref BufferLookup<Game.Net.SubLane> subLanes)
	{
		PathElement pathElement = path[elementIndex];
		if (!edgeLaneData.HasComponent(pathElement.m_Target))
		{
			Lane lane = laneData[pathElement.m_Target];
			bool flag = pathElement.m_TargetDelta.y < 0.5f;
			if (!NetUtils.FindEdgeLane(ref pathElement.m_Target, ref ownerData, ref laneData, ref subLanes, flag))
			{
				return;
			}
			pathElement.m_TargetDelta = (flag ? lane.m_StartNode.GetCurvePos() : lane.m_EndNode.GetCurvePos());
			path.Insert(elementIndex + 1, pathElement);
		}
		Curve curve = curveData[pathElement.m_Target];
		Entity entity = pathElement.m_Target;
		float t;
		float num = MathUtils.Distance(curve.m_Bezier, position, out t) + random.NextFloat(0.5f);
		Entity entity2 = pathElement.m_Target;
		if (NetUtils.FindPrevLane(ref entity2, ref ownerData, ref laneData, ref subLanes))
		{
			float t2;
			float num2 = MathUtils.Distance(curveData[entity2].m_Bezier, position, out t2) + random.NextFloat(0.5f);
			if (num2 < num)
			{
				entity = entity2;
				num = num2;
				t = t2;
			}
		}
		Entity entity3 = pathElement.m_Target;
		if (NetUtils.FindNextLane(ref entity3, ref ownerData, ref laneData, ref subLanes))
		{
			float t3;
			float num3 = MathUtils.Distance(curveData[entity3].m_Bezier, position, out t3) + random.NextFloat(0.5f);
			if (num3 < num)
			{
				entity = entity3;
				num = num3;
				t = t3;
			}
		}
		curve = curveData[entity];
		float num4 = random.NextFloat(-0.5f, 0.5f);
		if (num4 >= 0f)
		{
			Bounds1 t4 = new Bounds1(t, 1f);
			t = ((!MathUtils.ClampLength(curve.m_Bezier.xz, ref t4, num4)) ? math.saturate(t + (1f - t) * num4 / 0.5f) : t4.max);
		}
		else
		{
			num4 = 0f - num4;
			Bounds1 t5 = new Bounds1(0f, t);
			t = ((!MathUtils.ClampLengthInverse(curve.m_Bezier.xz, ref t5, num4)) ? math.saturate(t - t * num4 / 0.5f) : t5.min);
		}
		if (entity == pathElement.m_Target)
		{
			pathElement.m_TargetDelta.y = t;
			path[elementIndex] = pathElement;
		}
		else if (entity == entity3)
		{
			path.Insert(elementIndex + 1, new PathElement
			{
				m_Target = entity3,
				m_TargetDelta = new float2(0f, t)
			});
			pathElement.m_TargetDelta.y = 1f;
			path[elementIndex] = pathElement;
		}
		else if (entity == entity2)
		{
			path.Insert(elementIndex + 1, new PathElement
			{
				m_Target = entity2,
				m_TargetDelta = new float2(1f, t)
			});
			pathElement.m_TargetDelta.y = 0f;
			path[elementIndex] = pathElement;
		}
	}

	public static void SetRandomAreaTarget(ref Unity.Mathematics.Random random, int elementIndex, DynamicBuffer<PathElement> path, ComponentLookup<Owner> ownerData, ComponentLookup<Curve> curveData, ComponentLookup<Lane> laneData, ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, BufferLookup<Game.Net.SubLane> subLanes, BufferLookup<Game.Areas.Node> areaNodes, BufferLookup<Triangle> areaTriangles)
	{
		Entity owner = ownerData[path[elementIndex].m_Target].m_Owner;
		DynamicBuffer<Game.Areas.Node> nodes = areaNodes[owner];
		DynamicBuffer<Triangle> dynamicBuffer = areaTriangles[owner];
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			float num3 = MathUtils.Area(AreaUtils.GetTriangle3(nodes, dynamicBuffer[i]).xz);
			num2 += num3;
			if (random.NextFloat(num2) < num3)
			{
				num = i;
			}
		}
		if (num == -1)
		{
			return;
		}
		DynamicBuffer<Game.Net.SubLane> lanes = subLanes[owner];
		float2 @float = random.NextFloat2(1f);
		@float = math.select(@float, 1f - @float, math.csum(@float) > 1f);
		Triangle3 triangle = AreaUtils.GetTriangle3(nodes, dynamicBuffer[num]);
		float3 position = MathUtils.Position(triangle, @float);
		float num4 = float.MaxValue;
		Entity entity = Entity.Null;
		float endCurvePos = 0f;
		for (int j = 0; j < lanes.Length; j++)
		{
			Entity subLane = lanes[j].m_SubLane;
			if (!connectionLaneData.HasComponent(subLane) || (connectionLaneData[subLane].m_Flags & ConnectionLaneFlags.Pedestrian) == 0)
			{
				continue;
			}
			Curve curve = curveData[subLane];
			float2 t;
			bool2 x = new bool2(MathUtils.Intersect(triangle.xz, curve.m_Bezier.a.xz, out t), MathUtils.Intersect(triangle.xz, curve.m_Bezier.d.xz, out t));
			if (math.any(x))
			{
				float t2;
				float num5 = MathUtils.Distance(curve.m_Bezier, position, out t2);
				if (num5 < num4)
				{
					float2 float2 = math.select(new float2(0f, 0.49f), math.select(new float2(0.51f, 1f), new float2(0f, 1f), x.x), x.y);
					num4 = num5;
					entity = subLane;
					endCurvePos = random.NextFloat(float2.x, float2.y);
				}
			}
		}
		if (entity == Entity.Null)
		{
			return;
		}
		int num6 = elementIndex;
		Owner componentData;
		while (num6 > 0 && ownerData.TryGetComponent(path[num6 - 1].m_Target, out componentData) && !(componentData.m_Owner != owner))
		{
			num6--;
		}
		NativeList<PathElement> path2 = new NativeList<PathElement>(lanes.Length, Allocator.Temp);
		PathElement pathElement = path[num6];
		AreaUtils.FindAreaPath(ref random, path2, lanes, pathElement.m_Target, pathElement.m_TargetDelta.x, entity, endCurvePos, laneData, curveData);
		if (path2.Length != 0)
		{
			int num7 = elementIndex - num6 + 1;
			int num8 = math.min(num7, path2.Length);
			for (int k = 0; k < num8; k++)
			{
				path[num6 + k] = path2[k];
			}
			if (path2.Length < num7)
			{
				path.RemoveRange(num6 + path2.Length, num7 - path2.Length);
			}
			else
			{
				for (int l = num7; l < path2.Length; l++)
				{
					path.Insert(num6 + l, path2[l]);
				}
			}
		}
		path2.Dispose();
	}

	public static void CheckUnspawned(int jobIndex, Entity entity, HumanCurrentLane currentLane, Human human, bool isUnspawned, EntityCommandBuffer.ParallelWriter commandBuffer)
	{
		if ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0 || (human.m_Flags & (HumanFlags.Dead | HumanFlags.Carried)) != 0)
		{
			if (!isUnspawned)
			{
				commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
				commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
			}
		}
		else if ((currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0 && currentLane.m_Lane != Entity.Null && isUnspawned)
		{
			commandBuffer.RemoveComponent<Unspawned>(jobIndex, entity);
			commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
		}
	}

	public static void CheckUnspawned(int jobIndex, Entity entity, AnimalCurrentLane currentLane, Animal animal, bool isUnspawned, EntityCommandBuffer.ParallelWriter commandBuffer)
	{
		if ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0)
		{
			if (!isUnspawned)
			{
				commandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
				commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
			}
		}
		else if ((currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0 && (currentLane.m_Lane != Entity.Null || (animal.m_Flags & AnimalFlags.Roaming) != 0) && isUnspawned)
		{
			commandBuffer.RemoveComponent<Unspawned>(jobIndex, entity);
			commandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
		}
	}
}
