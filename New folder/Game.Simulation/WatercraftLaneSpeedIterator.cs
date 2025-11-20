using System;
using Colossal.Mathematics;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct WatercraftLaneSpeedIterator
{
	public ComponentLookup<Transform> m_TransformData;

	public ComponentLookup<Moving> m_MovingData;

	public ComponentLookup<Watercraft> m_WatercraftData;

	public ComponentLookup<LaneReservation> m_LaneReservationData;

	public ComponentLookup<LaneSignal> m_LaneSignalData;

	public ComponentLookup<Curve> m_CurveData;

	public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

	public ComponentLookup<PrefabRef> m_PrefabRefData;

	public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

	public ComponentLookup<WatercraftData> m_PrefabWatercraftData;

	public BufferLookup<LaneOverlap> m_LaneOverlapData;

	public BufferLookup<LaneObject> m_LaneObjectData;

	public Entity m_Entity;

	public Entity m_Ignore;

	public int m_Priority;

	public float m_TimeStep;

	public float m_SafeTimeStep;

	public float m_SpeedLimitFactor;

	public float m_CurrentSpeed;

	public WatercraftData m_PrefabWatercraft;

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
		float3 currentPosition = MathUtils.Position(curve.m_Bezier, curveOffset.x);
		m_PrevPosition = m_CurrentPosition;
		m_Distance = math.distance(m_CurrentPosition.xz, currentPosition.xz);
		if (m_CarLaneData.HasComponent(lane))
		{
			Game.Net.CarLane carLaneData = m_CarLaneData[lane];
			carLaneData.m_SpeedLimit *= m_SpeedLimitFactor;
			float num = VehicleUtils.GetMaxDriveSpeed(m_PrefabWatercraft, carLaneData);
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
			m_CurrentPosition = currentPosition;
			CheckCurrentLane(num3, xy);
			CheckOverlappingLanes(num3, xy.y);
		}
		float3 currentPosition2 = MathUtils.Position(curve.m_Bezier, curveOffset.z);
		float num4 = math.abs(curveOffset.z - curveOffset.x);
		float num5 = math.max(0.001f, math.lerp(math.distance(currentPosition.xz, currentPosition2.xz), curve.m_Length * num4, num4));
		if (num5 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = currentPosition2;
		m_Distance += num5;
		float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabWatercraft, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance - 225f >= brakingDistance) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public bool IterateFirstLane(Entity lane1, Entity lane2, float3 curveOffset, float laneDelta)
	{
		laneDelta = math.saturate(laneDelta);
		Curve curve = m_CurveData[lane1];
		Curve curve2 = m_CurveData[lane2];
		float3 @float = MathUtils.Position(curve.m_Bezier, curveOffset.x);
		float3 float2 = MathUtils.Position(curve2.m_Bezier, curveOffset.x);
		float3 float3 = math.lerp(@float, float2, laneDelta);
		m_PrevPosition = m_CurrentPosition;
		m_Distance = math.distance(m_CurrentPosition.xz, float3.xz);
		if (m_CarLaneData.HasComponent(lane1))
		{
			Game.Net.CarLane carLaneData = m_CarLaneData[lane1];
			carLaneData.m_SpeedLimit *= m_SpeedLimitFactor;
			float num = VehicleUtils.GetMaxDriveSpeed(m_PrefabWatercraft, carLaneData);
			if (m_Priority < 102 && m_LaneReservationData.HasComponent(lane1) && m_LaneReservationData.HasComponent(lane2))
			{
				if (laneDelta < 0.9f)
				{
					LaneReservation laneReservation = m_LaneReservationData[lane1];
					_ = m_LaneReservationData[lane2];
					if (math.any(new int2(laneReservation.GetPriority() == 102)))
					{
						num *= 0.5f;
					}
				}
				else if (m_LaneReservationData[lane2].GetPriority() == 102)
				{
					num *= 0.5f;
				}
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
			if (laneDelta < 0.9f)
			{
				m_Lane = lane1;
				m_Curve = curve;
				m_CurveOffset = curveOffset.xz;
				m_CurrentPosition = @float;
				CheckCurrentLane(num3, xy);
				CheckOverlappingLanes(num3, xy.y);
			}
			m_Lane = lane2;
			m_Curve = curve2;
			m_CurveOffset = curveOffset.xz;
			m_CurrentPosition = float2;
			if (laneDelta == 0f)
			{
				CheckCurrentLane(num3, xy, ref m_CanChangeLane);
			}
			else
			{
				CheckCurrentLane(num3, xy);
			}
			CheckOverlappingLanes(num3, xy.y);
		}
		float3 currentPosition = MathUtils.Position(curve2.m_Bezier, curveOffset.z);
		float num4 = math.lerp(curve.m_Length, curve2.m_Length, laneDelta);
		float num5 = math.abs(curveOffset.z - curveOffset.x);
		float num6 = math.max(0.001f, math.lerp(math.distance(float3.xz, currentPosition.xz), num4 * num5, num5));
		if (num6 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = currentPosition;
		m_Distance += num6;
		float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabWatercraft, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance - 225f >= brakingDistance) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public bool IterateNextLane(Entity lane, float2 curveOffset, float minOffset, bool ignoreSignal, out bool needSignal)
	{
		needSignal = false;
		if (!m_CurveData.TryGetComponent(lane, out var componentData))
		{
			return false;
		}
		if (m_CarLaneData.TryGetComponent(lane, out var componentData2))
		{
			componentData2.m_SpeedLimit *= m_SpeedLimitFactor;
			float num = VehicleUtils.GetMaxDriveSpeed(m_PrefabWatercraft, componentData2);
			float num2 = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num3 = m_Distance + num2;
			Entity blocker = Entity.Null;
			BlockerType blockerType = BlockerType.Limit;
			if ((componentData2.m_Flags & Game.Net.CarLaneFlags.Approach) == 0 && (componentData2.m_Flags & Game.Net.CarLaneFlags.LevelCrossing) != 0 && !ignoreSignal && m_LaneSignalData.TryGetComponent(lane, out var componentData3))
			{
				float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabWatercraft, m_CurrentSpeed, 0f);
				needSignal = true;
				switch (componentData3.m_Signal)
				{
				case LaneSignalType.Stop:
					if ((m_Priority < 108 || (componentData3.m_Flags & LaneSignalFlags.Physical) != 0) && brakingDistance <= num3 + 1f)
					{
						num = 0f;
						blocker = componentData3.m_Blocker;
						blockerType = BlockerType.Signal;
					}
					break;
				case LaneSignalType.SafeStop:
					if ((m_Priority < 108 || (componentData3.m_Flags & LaneSignalFlags.Physical) != 0) && brakingDistance <= num3)
					{
						num = 0f;
						blocker = componentData3.m_Blocker;
						blockerType = BlockerType.Signal;
					}
					break;
				}
			}
			float num4 = ((num != 0f) ? math.max(num, VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, m_Distance, num, m_TimeStep)) : VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, num3, m_SafeTimeStep));
			if (num4 < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(num4, m_SpeedRange);
				m_Blocker = blocker;
				m_BlockerType = blockerType;
			}
			m_Curve = componentData;
			m_CurveOffset = curveOffset;
			m_Lane = lane;
			minOffset = math.select(minOffset, curveOffset.x, curveOffset.x > 0f);
			CheckCurrentLane(num3, minOffset);
			CheckOverlappingLanes(num3, minOffset);
		}
		float3 currentPosition = MathUtils.Position(componentData.m_Bezier, curveOffset.y);
		float num5 = math.abs(curveOffset.y - curveOffset.x);
		float num6 = math.max(0.001f, math.lerp(math.distance(m_CurrentPosition.xz, currentPosition.xz), componentData.m_Length * num5, num5));
		if (num6 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = currentPosition;
		m_Distance += num6;
		float brakingDistance2 = VehicleUtils.GetBrakingDistance(m_PrefabWatercraft, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance - 225f >= brakingDistance2) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public void IterateTarget(float3 targetPosition)
	{
		float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(m_PrefabWatercraft, 11.111112f, MathF.PI / 12f);
		IterateTarget(targetPosition, maxDriveSpeed);
	}

	public void IterateTarget(float3 targetPosition, float maxLaneSpeed)
	{
		float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, m_Distance, maxLaneSpeed, m_TimeStep);
		m_Distance += math.distance(m_CurrentPosition.xz, targetPosition.xz);
		maxBrakingSpeed = math.min(maxBrakingSpeed, VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, m_Distance, m_TimeStep));
		if (maxBrakingSpeed < m_MaxSpeed)
		{
			m_MaxSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			m_Blocker = Entity.Null;
			m_BlockerType = BlockerType.None;
		}
	}

	private void CheckCurrentLane(float distance, float2 minOffset)
	{
		if (!m_LaneObjectData.TryGetBuffer(m_Lane, out var bufferData) || bufferData.Length == 0)
		{
			return;
		}
		float3 currentPos = MathUtils.Position(m_Curve.m_Bezier, m_CurveOffset.x);
		distance -= 1f + m_PrefabObjectGeometry.m_Size.x;
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneObject laneObject = bufferData[i];
			if (!(laneObject.m_LaneObject == m_Entity))
			{
				float2 curvePosition = laneObject.m_CurvePosition;
				if (!(curvePosition.y <= minOffset.y) || (!(curvePosition.y < 1f) && !(curvePosition.x <= minOffset.x)))
				{
					float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
					UpdateMaxSpeed(laneObject.m_LaneObject, BlockerType.Continuing, objectSpeed, curvePosition.x, 1f, distance, 0f, laneObject.m_LaneObject == m_Ignore, currentPos);
				}
			}
		}
	}

	private void CheckCurrentLane(float distance, float2 minOffset, ref float canUseLane)
	{
		if (!m_LaneObjectData.TryGetBuffer(m_Lane, out var bufferData) || bufferData.Length == 0)
		{
			return;
		}
		float3 currentPos = MathUtils.Position(m_Curve.m_Bezier, m_CurveOffset.x);
		distance -= 1f + m_PrefabObjectGeometry.m_Size.x;
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneObject laneObject = bufferData[i];
			if (laneObject.m_LaneObject == m_Entity)
			{
				continue;
			}
			float2 curvePosition = laneObject.m_CurvePosition;
			if (curvePosition.y <= minOffset.y && (curvePosition.y < 1f || curvePosition.x <= minOffset.x))
			{
				PrefabRef prefabRef = m_PrefabRefData[laneObject.m_LaneObject];
				float num = 0f;
				if (m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					num = 0f - m_PrefabObjectGeometryData[prefabRef.m_Prefab].m_Bounds.max.z;
				}
				if ((curvePosition.x - minOffset.x) * m_Curve.m_Length > num)
				{
					canUseLane = 0f;
				}
			}
			else
			{
				float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
				UpdateMaxSpeed(laneObject.m_LaneObject, BlockerType.Continuing, objectSpeed, curvePosition.x, 1f, distance, 0f, laneObject.m_LaneObject == m_Ignore, currentPos);
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
			if ((laneOverlap.m_Flags & OverlapFlags.Water) == 0)
			{
				continue;
			}
			float4 @float = new float4((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd, (int)laneOverlap.m_OtherStart, (int)laneOverlap.m_OtherEnd) * 0.003921569f;
			if (@float.y <= curveOffset.x)
			{
				continue;
			}
			m_Lane = laneOverlap.m_Other;
			m_Curve = m_CurveData[m_Lane];
			m_CurveOffset = @float.zw;
			float3 currentPos = MathUtils.Position(m_Curve.m_Bezier, m_CurveOffset.x);
			float num = math.max(0f, origMinOffset - @float.x) + @float.z;
			float num2 = length * (@float.x - curveOffset.x);
			float num3 = origDistance + num2;
			float distanceFactor = (float)(int)laneOverlap.m_Parallelism * (1f / 128f);
			int num4 = priority;
			BlockerType blockerType = (((laneOverlap.m_Flags & (OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleEnd)) != 0) ? BlockerType.Continuing : BlockerType.Crossing);
			num3 -= m_PrefabObjectGeometry.m_Size.x;
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
						float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, num3, m_SafeTimeStep);
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
			if (!m_LaneObjectData.TryGetBuffer(m_Lane, out var bufferData))
			{
				continue;
			}
			for (int j = 0; j < bufferData.Length; j++)
			{
				LaneObject laneObject = bufferData[j];
				float2 curvePosition = laneObject.m_CurvePosition;
				float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
				if ((laneOverlap.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeMiddleStart)) == 0 && (@float.x >= origMinOffset || curvePosition.y > @float.z))
				{
					int num5;
					if (m_WatercraftData.HasComponent(laneObject.m_LaneObject))
					{
						PrefabRef prefabRef = m_PrefabRefData[laneObject.m_LaneObject];
						num5 = VehicleUtils.GetPriority(m_PrefabWatercraftData[prefabRef.m_Prefab]);
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
					UpdateMaxSpeed(laneObject.m_LaneObject, blockerType, objectSpeed, curvePosition.x, distanceFactor, num3, num2, laneObject.m_LaneObject == m_Ignore, currentPos);
				}
			}
		}
	}

	private float GetObjectSpeed(Entity obj, float curveOffset)
	{
		if (!m_MovingData.TryGetComponent(obj, out var componentData))
		{
			return 0f;
		}
		float2 y = math.normalizesafe(MathUtils.Tangent(m_Curve.m_Bezier, curveOffset).xz);
		return math.dot(componentData.m_Velocity.xz, y) * 0.75f;
	}

	private void UpdateMaxSpeed(Entity obj, BlockerType blockerType, float objectSpeed, float laneOffset, float distanceFactor, float distanceOffset, float overlapOffset, bool ignore, float3 currentPos)
	{
		PrefabRef prefabRef = m_PrefabRefData[obj];
		float num = 0f;
		if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
		{
			num = math.max(0f, 0f - componentData.m_Bounds.min.z);
		}
		if ((laneOffset - m_CurveOffset.y) * m_Curve.m_Length >= num)
		{
			return;
		}
		Transform transform = m_TransformData[obj];
		float3 @float = MathUtils.Position(m_Curve.m_Bezier, math.max(m_CurveOffset.x, laneOffset));
		if (m_PrefabWatercraftData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
		{
			VehicleUtils.CalculateShipNavigationPivots(transform, componentData, out var pivot, out var pivot2);
			transform.m_Position = pivot;
			float num2 = math.distance(pivot.xz, pivot2.xz);
			num += num2 * 0.5f;
		}
		float num3 = math.distance(@float.xz, currentPos.xz);
		num3 -= math.max(0f, m_CurveOffset.x - laneOffset) * m_Curve.m_Length;
		num3 = ((!(math.dot(transform.m_Position.xz - currentPos.xz, currentPos.xz - m_PrevPosition.xz) < 0f)) ? math.min(num3, math.distance(transform.m_Position.xz, currentPos.xz)) : math.min(num3, math.distance(transform.m_Position.xz, m_PrevPosition.xz) + m_PrevDistance - m_Distance - math.min(0f, overlapOffset)));
		num3 -= num;
		num3 *= distanceFactor;
		num3 += distanceOffset;
		float maxBrakingSpeed;
		if (objectSpeed > 0.001f && componentData2.m_MaxSpeed != 0f)
		{
			objectSpeed = math.max(0f, objectSpeed - componentData2.m_Braking * m_TimeStep * 2f) * distanceFactor;
			if (m_PrefabWatercraft.m_Braking >= componentData2.m_Braking)
			{
				num3 += objectSpeed * m_SafeTimeStep;
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, num3, objectSpeed, m_SafeTimeStep);
			}
			else
			{
				num3 += VehicleUtils.GetBrakingDistance(componentData2, objectSpeed, m_SafeTimeStep);
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, num3, m_SafeTimeStep);
			}
		}
		else
		{
			maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabWatercraft, num3, m_SafeTimeStep);
		}
		maxBrakingSpeed = math.select(maxBrakingSpeed, 1f, ignore && maxBrakingSpeed < 1f);
		maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
		if (maxBrakingSpeed < m_MaxSpeed)
		{
			m_MaxSpeed = maxBrakingSpeed;
			m_Blocker = obj;
			m_BlockerType = blockerType;
		}
	}
}
