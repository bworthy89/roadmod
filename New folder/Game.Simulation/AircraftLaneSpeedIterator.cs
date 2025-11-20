using System;
using Colossal.Mathematics;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct AircraftLaneSpeedIterator
{
	public ComponentLookup<Transform> m_TransformData;

	public ComponentLookup<Moving> m_MovingData;

	public ComponentLookup<Aircraft> m_AircraftData;

	public ComponentLookup<LaneReservation> m_LaneReservationData;

	public ComponentLookup<Curve> m_CurveData;

	public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

	public ComponentLookup<PrefabRef> m_PrefabRefData;

	public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

	public ComponentLookup<AircraftData> m_PrefabAircraftData;

	public BufferLookup<LaneOverlap> m_LaneOverlapData;

	public BufferLookup<LaneObject> m_LaneObjectData;

	public Entity m_Entity;

	public Entity m_Ignore;

	public int m_Priority;

	public float m_TimeStep;

	public float m_SafeTimeStep;

	public AircraftData m_PrefabAircraft;

	public ObjectGeometryData m_PrefabObjectGeometry;

	public Bounds1 m_SpeedRange;

	public float m_MaxSpeed;

	public float m_CanChangeLane;

	public float3 m_CurrentPosition;

	public float m_Distance;

	public Entity m_Blocker;

	public BlockerType m_BlockerType;

	private Entity m_Lane;

	private Curve m_Curve;

	private float2 m_CurveOffset;

	private float3 m_PrevPosition;

	private float m_PrevDistance;

	public bool IterateFirstLane(Entity lane, float3 curveOffset)
	{
		Curve curve = m_CurveData[lane];
		float3 @float = MathUtils.Position(curve.m_Bezier, curveOffset.x);
		m_PrevPosition = m_CurrentPosition;
		m_Distance = math.distance(m_CurrentPosition.xz, @float.xz);
		if (m_CarLaneData.HasComponent(lane))
		{
			Game.Net.CarLane carLaneData = m_CarLaneData[lane];
			float num = VehicleUtils.GetMaxDriveSpeed(m_PrefabAircraft, carLaneData);
			if (m_Priority < 102 && m_LaneReservationData.HasComponent(lane) && m_LaneReservationData[lane].GetPriority() == 102)
			{
				num *= 0.5f;
			}
			if (num < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(num, m_SpeedRange);
				m_Blocker = Entity.Null;
				m_BlockerType = BlockerType.Limit;
			}
			float2 xy = curveOffset.xy;
			float num2 = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num3 = m_Distance + num2;
			m_Lane = lane;
			m_Curve = curve;
			m_CurveOffset = curveOffset.xz;
			m_CurrentPosition = @float;
			CheckCurrentLane(num3, xy);
			CheckOverlappingLanes(num3, xy.y);
		}
		float3 float2 = MathUtils.Position(curve.m_Bezier, curveOffset.z);
		float num4 = math.abs(curveOffset.z - curveOffset.x);
		float num5 = math.max(0.001f, math.lerp(math.distance(@float, float2), curve.m_Length * num4, num4));
		if (num5 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = float2;
		m_Distance += num5;
		float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabAircraft, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance - 20f >= brakingDistance) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public bool IterateNextLane(Entity lane, float2 curveOffset, float minOffset)
	{
		if (!m_CurveData.HasComponent(lane))
		{
			return false;
		}
		Curve curve = m_CurveData[lane];
		if (m_CarLaneData.HasComponent(lane))
		{
			Game.Net.CarLane carLaneData = m_CarLaneData[lane];
			float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(m_PrefabAircraft, carLaneData);
			float num = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num2 = m_Distance + num;
			float num3 = ((maxDriveSpeed != 0f) ? math.max(maxDriveSpeed, VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, m_Distance, maxDriveSpeed, m_TimeStep)) : VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, num2, m_SafeTimeStep));
			if (num3 < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(num3, m_SpeedRange);
				m_Blocker = Entity.Null;
				m_BlockerType = BlockerType.Limit;
			}
			m_Curve = curve;
			m_CurveOffset = curveOffset;
			m_Lane = lane;
			minOffset = math.select(minOffset, curveOffset.x, curveOffset.x > 0f);
			CheckCurrentLane(num2, minOffset);
			CheckOverlappingLanes(num2, minOffset);
		}
		float3 @float = MathUtils.Position(curve.m_Bezier, curveOffset.y);
		float num4 = math.abs(curveOffset.y - curveOffset.x);
		float num5 = math.max(0.001f, math.lerp(math.distance(m_CurrentPosition, @float), curve.m_Length * num4, num4));
		if (num5 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = @float;
		m_Distance += num5;
		float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabAircraft, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance - 20f >= brakingDistance) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public void IterateTarget(float3 targetPosition)
	{
		float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(m_PrefabAircraft, 11.111112f, MathF.PI / 12f);
		float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, m_Distance, maxDriveSpeed, m_TimeStep);
		m_Distance += math.distance(m_CurrentPosition.xz, targetPosition.xz);
		maxBrakingSpeed = math.min(maxBrakingSpeed, VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, m_Distance, m_TimeStep));
		if (maxBrakingSpeed < m_MaxSpeed)
		{
			m_MaxSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			m_Blocker = Entity.Null;
			m_BlockerType = BlockerType.None;
		}
	}

	private void CheckCurrentLane(float distance, float2 minOffset)
	{
		if (!m_LaneObjectData.HasBuffer(m_Lane))
		{
			return;
		}
		DynamicBuffer<LaneObject> dynamicBuffer = m_LaneObjectData[m_Lane];
		if (dynamicBuffer.Length == 0)
		{
			return;
		}
		distance -= 1f;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			LaneObject laneObject = dynamicBuffer[i];
			if (!(laneObject.m_LaneObject == m_Entity) && !(laneObject.m_LaneObject == m_Ignore))
			{
				float2 curvePosition = laneObject.m_CurvePosition;
				if (!(curvePosition.y <= minOffset.y) || (!(curvePosition.y < 1f) && !(curvePosition.x <= minOffset.x)))
				{
					float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
					UpdateMaxSpeed(laneObject.m_LaneObject, BlockerType.Continuing, objectSpeed, curvePosition.x, 1f, distance, 0f);
				}
			}
		}
	}

	private void CheckOverlappingLanes(float origDistance, float origMinOffset)
	{
		if (!m_LaneOverlapData.HasBuffer(m_Lane))
		{
			return;
		}
		DynamicBuffer<LaneOverlap> dynamicBuffer = m_LaneOverlapData[m_Lane];
		if (dynamicBuffer.Length == 0)
		{
			return;
		}
		origDistance -= 1f;
		float2 curveOffset = m_CurveOffset;
		float length = m_Curve.m_Length;
		int priority = m_Priority;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			LaneOverlap laneOverlap = dynamicBuffer[i];
			float4 @float = new float4((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd, (int)laneOverlap.m_OtherStart, (int)laneOverlap.m_OtherEnd) * 0.003921569f;
			if (@float.y <= curveOffset.x)
			{
				continue;
			}
			m_Lane = laneOverlap.m_Other;
			m_Curve = m_CurveData[m_Lane];
			m_CurveOffset = @float.zw;
			float num = math.max(0f, origMinOffset - @float.x) + @float.z;
			float num2 = length * (@float.x - curveOffset.x);
			float num3 = origDistance + num2;
			float distanceFactor = (float)(int)laneOverlap.m_Parallelism * (1f / 128f);
			int num4 = priority;
			BlockerType blockerType = (((laneOverlap.m_Flags & (OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleEnd)) != 0) ? BlockerType.Continuing : BlockerType.Crossing);
			if ((laneOverlap.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeMiddleStart)) == 0 && @float.x > origMinOffset)
			{
				num4 -= laneOverlap.m_PriorityDelta;
				if (m_LaneReservationData.HasComponent(m_Lane))
				{
					LaneReservation laneReservation = m_LaneReservationData[m_Lane];
					float offset = laneReservation.GetOffset();
					int priority2 = laneReservation.GetPriority();
					if (offset > math.max(num, m_CurveOffset.x) || priority2 > num4)
					{
						float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, num3, m_SafeTimeStep);
						maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
						if (maxBrakingSpeed < m_MaxSpeed)
						{
							m_MaxSpeed = maxBrakingSpeed;
							m_Blocker = Entity.Null;
							m_BlockerType = blockerType;
						}
					}
				}
			}
			if (!m_LaneObjectData.HasBuffer(m_Lane))
			{
				continue;
			}
			DynamicBuffer<LaneObject> dynamicBuffer2 = m_LaneObjectData[m_Lane];
			if (dynamicBuffer2.Length == 0)
			{
				continue;
			}
			m_CurrentPosition = MathUtils.Position(m_Curve.m_Bezier, m_CurveOffset.x);
			for (int j = 0; j < dynamicBuffer2.Length; j++)
			{
				LaneObject laneObject = dynamicBuffer2[j];
				if (laneObject.m_LaneObject == m_Ignore)
				{
					continue;
				}
				float2 curvePosition = laneObject.m_CurvePosition;
				float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
				if ((laneOverlap.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeMiddleStart)) == 0 && (@float.x >= origMinOffset || curvePosition.y > @float.z))
				{
					int num5;
					if (m_AircraftData.HasComponent(laneObject.m_LaneObject))
					{
						PrefabRef prefabRef = m_PrefabRefData[laneObject.m_LaneObject];
						num5 = VehicleUtils.GetPriority(m_PrefabAircraftData[prefabRef.m_Prefab]);
					}
					else
					{
						num5 = 0;
					}
					int num6 = num5 - num4;
					if (num6 > 0)
					{
						curvePosition.y += objectSpeed * 2f / math.max(1f, m_Curve.m_Length);
					}
					else if (num6 < 0)
					{
						curvePosition.y -= math.max(0f, @float.z - num);
					}
				}
				if (!(curvePosition.y <= num))
				{
					UpdateMaxSpeed(laneObject.m_LaneObject, blockerType, objectSpeed, curvePosition.x, distanceFactor, num3, num2);
				}
			}
		}
	}

	private float GetObjectSpeed(Entity obj, float curveOffset)
	{
		if (!m_MovingData.HasComponent(obj))
		{
			return 0f;
		}
		Moving moving = m_MovingData[obj];
		return math.dot(y: math.normalizesafe(MathUtils.Tangent(m_Curve.m_Bezier, curveOffset)), x: moving.m_Velocity);
	}

	private void UpdateMaxSpeed(Entity obj, BlockerType blockerType, float objectSpeed, float laneOffset, float distanceFactor, float distanceOffset, float overlapOffset)
	{
		PrefabRef prefabRef = m_PrefabRefData[obj];
		float num = 0f;
		if (m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
		{
			num = math.max(0f, 0f - m_PrefabObjectGeometryData[prefabRef.m_Prefab].m_Bounds.min.z);
		}
		if ((laneOffset - m_CurveOffset.y) * m_Curve.m_Length >= num)
		{
			return;
		}
		Transform transform = m_TransformData[obj];
		float num2 = math.distance(MathUtils.Position(m_Curve.m_Bezier, math.max(m_CurveOffset.x, laneOffset)).xz, m_CurrentPosition.xz);
		num2 -= math.max(0f, m_CurveOffset.x - laneOffset) * m_Curve.m_Length;
		num2 = ((!(math.dot(transform.m_Position.xz - m_CurrentPosition.xz, m_CurrentPosition.xz - m_PrevPosition.xz) < 0f)) ? math.min(num2, math.distance(transform.m_Position.xz, m_CurrentPosition.xz)) : math.min(num2, math.distance(transform.m_Position.xz, m_PrevPosition.xz) + m_PrevDistance - m_Distance - math.min(0f, overlapOffset)));
		num2 -= num;
		num2 *= distanceFactor;
		num2 += distanceOffset;
		float maxBrakingSpeed;
		if (objectSpeed > 0.001f && m_PrefabAircraftData.HasComponent(prefabRef.m_Prefab))
		{
			AircraftData prefabAircraftData = m_PrefabAircraftData[prefabRef.m_Prefab];
			objectSpeed = math.max(0f, objectSpeed - prefabAircraftData.m_GroundBraking * m_TimeStep * 2f) * distanceFactor;
			if (m_PrefabAircraft.m_GroundBraking >= prefabAircraftData.m_GroundBraking)
			{
				num2 += objectSpeed * m_SafeTimeStep;
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, num2, objectSpeed, m_SafeTimeStep);
			}
			else
			{
				num2 += VehicleUtils.GetBrakingDistance(prefabAircraftData, objectSpeed, m_SafeTimeStep);
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, num2, m_SafeTimeStep);
			}
		}
		else
		{
			maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabAircraft, num2, m_SafeTimeStep);
		}
		maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
		if (maxBrakingSpeed < m_MaxSpeed)
		{
			m_MaxSpeed = maxBrakingSpeed;
			m_Blocker = obj;
			m_BlockerType = blockerType;
		}
	}
}
