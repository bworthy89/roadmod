using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public static class AircraftNavigationHelpers
{
	public struct LaneReservation : IComparable<LaneReservation>
	{
		public Entity m_Lane;

		public byte m_Offset;

		public byte m_Priority;

		public LaneReservation(Entity lane, float offset, int priority)
		{
			m_Lane = lane;
			m_Offset = (byte)math.clamp((int)math.round(offset * 255f), 0, 255);
			m_Priority = (byte)priority;
		}

		public int CompareTo(LaneReservation other)
		{
			return m_Lane.Index - other.m_Lane.Index;
		}
	}

	public struct LaneEffects
	{
		public Entity m_Lane;

		public float3 m_SideEffects;

		public float m_RelativeSpeed;

		public LaneEffects(Entity lane, float3 sideEffects, float relativeSpeed)
		{
			m_Lane = lane;
			m_SideEffects = sideEffects;
			m_RelativeSpeed = relativeSpeed;
		}
	}

	public struct CurrentLaneCache
	{
		private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		private Entity m_WasCurrentLane;

		private float2 m_WasCurvePosition;

		private bool m_WasFlying;

		public CurrentLaneCache(ref AircraftCurrentLane currentLane, ComponentLookup<PrefabRef> prefabRefData, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree)
		{
			if (!prefabRefData.HasComponent(currentLane.m_Lane))
			{
				currentLane.m_Lane = Entity.Null;
			}
			m_SearchTree = searchTree;
			m_WasCurrentLane = currentLane.m_Lane;
			m_WasCurvePosition = currentLane.m_CurvePosition.xy;
			m_WasFlying = (currentLane.m_LaneFlags & AircraftLaneFlags.Flying) != 0;
		}

		public void CheckChanges(Entity entity, ref AircraftCurrentLane currentLane, LaneObjectCommandBuffer buffer, BufferLookup<LaneObject> laneObjects, Transform transform, Moving moving, AircraftNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			bool2 x = new bool2(m_WasFlying, (currentLane.m_LaneFlags & AircraftLaneFlags.Flying) != 0);
			if (currentLane.m_Lane != m_WasCurrentLane)
			{
				if (laneObjects.HasBuffer(m_WasCurrentLane))
				{
					buffer.Remove(m_WasCurrentLane, entity);
				}
				else
				{
					x.x = true;
				}
				if (laneObjects.HasBuffer(currentLane.m_Lane))
				{
					buffer.Add(currentLane.m_Lane, entity, currentLane.m_CurvePosition.xy);
				}
				else
				{
					x.y = true;
				}
			}
			else if (laneObjects.HasBuffer(m_WasCurrentLane))
			{
				if (!m_WasCurvePosition.Equals(currentLane.m_CurvePosition.xy))
				{
					buffer.Update(m_WasCurrentLane, entity, currentLane.m_CurvePosition.xy);
				}
			}
			else
			{
				x = true;
			}
			if (!math.any(x))
			{
				return;
			}
			if (math.all(x))
			{
				if (m_SearchTree.TryGet(entity, out var bounds))
				{
					Bounds3 bounds2 = CalculateMinBounds(transform, moving, navigation, objectGeometryData);
					if (math.any(bounds2.min < bounds.m_Bounds.min) | math.any(bounds2.max > bounds.m_Bounds.max))
					{
						buffer.Update(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
					}
				}
			}
			else if (x.x)
			{
				buffer.Remove(entity);
			}
			else
			{
				buffer.Add(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
			}
		}

		private Bounds3 CalculateMinBounds(Transform transform, Moving moving, AircraftNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			float num = 4f / 15f;
			float3 x = moving.m_Velocity * num;
			float3 y = math.normalizesafe(navigation.m_TargetPosition - transform.m_Position) * (navigation.m_MaxSpeed * num);
			Bounds3 result = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData);
			result.min += math.min(0f, math.min(x, y));
			result.max += math.max(0f, math.max(x, y));
			return result;
		}

		private Bounds3 CalculateMaxBounds(Transform transform, Moving moving, AircraftNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			float num = -1.0666667f;
			float num2 = 2f;
			float num3 = math.length(objectGeometryData.m_Size) * 0.5f;
			float3 x = moving.m_Velocity * num;
			float3 x2 = moving.m_Velocity * num2;
			float3 y = math.normalizesafe(navigation.m_TargetPosition - transform.m_Position) * (navigation.m_MaxSpeed * num2);
			float3 position = transform.m_Position;
			position.y += objectGeometryData.m_Size.y * 0.5f;
			Bounds3 result = default(Bounds3);
			result.min = position - num3 + math.min(x, math.min(x2, y));
			result.max = position + num3 + math.max(x, math.max(x2, y));
			return result;
		}
	}

	public struct FindLaneIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Bounds3 m_Bounds;

		public float3 m_Position;

		public float m_MinDistance;

		public AircraftCurrentLane m_Result;

		public RoadTypes m_CarType;

		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		public ComponentLookup<Curve> m_CurveData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_SubLanes.HasBuffer(edgeEntity))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[edgeEntity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				AircraftLaneFlags laneFlags = m_Result.m_LaneFlags;
				laneFlags = (AircraftLaneFlags)((uint)laneFlags & 0xFFFDFFFBu);
				if (m_CarLaneData.HasComponent(subLane))
				{
					PrefabRef prefabRef = m_PrefabRefData[subLane];
					if ((m_PrefabCarLaneData[prefabRef.m_Prefab].m_RoadTypes & m_CarType) == 0)
					{
						continue;
					}
				}
				else
				{
					if (!m_ConnectionLaneData.HasComponent(subLane))
					{
						continue;
					}
					Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[subLane];
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0 || (connectionLane.m_RoadTypes & m_CarType) == 0)
					{
						continue;
					}
					laneFlags |= AircraftLaneFlags.Connection;
				}
				Bezier4x3 bezier = m_CurveData[subLane].m_Bezier;
				float num = MathUtils.Distance(MathUtils.Bounds(bezier), m_Position);
				if (num < m_MinDistance)
				{
					num = MathUtils.Distance(bezier, m_Position, out var t);
					if (num < m_MinDistance)
					{
						m_MinDistance = num;
						m_Result.m_Lane = subLane;
						m_Result.m_CurvePosition = t;
						m_Result.m_LaneFlags = laneFlags;
					}
				}
			}
		}

		public void Iterate(ref AirwayHelpers.AirwayData airwayData)
		{
			Entity lane = Entity.Null;
			float curvePos = 0f;
			float distance = math.select(m_MinDistance, float.MaxValue, m_Result.m_Lane == Entity.Null);
			if ((m_CarType & RoadTypes.Helicopter) != RoadTypes.None)
			{
				airwayData.helicopterMap.FindClosestLane(m_Position, m_CurveData, ref lane, ref curvePos, ref distance);
			}
			if ((m_CarType & RoadTypes.Airplane) != RoadTypes.None)
			{
				airwayData.airplaneMap.FindClosestLane(m_Position, m_CurveData, ref lane, ref curvePos, ref distance);
			}
			AircraftLaneFlags laneFlags = m_Result.m_LaneFlags;
			laneFlags = (AircraftLaneFlags)((uint)laneFlags & 0xFFFFFFFBu);
			if (lane != Entity.Null)
			{
				m_Result.m_Lane = lane;
				m_Result.m_CurvePosition = curvePos;
				m_Result.m_LaneFlags = laneFlags | AircraftLaneFlags.Airway;
				m_MinDistance = math.min(m_MinDistance, distance);
			}
		}
	}

	public struct AircraftCollisionIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_Ignore;

		public Line3.Segment m_Line;

		public ComponentLookup<Aircraft> m_AircraftData;

		public ComponentLookup<Transform> m_TransformData;

		public Entity m_Result;

		public float m_ClosestT;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			float2 t;
			return MathUtils.Intersect(bounds.m_Bounds, m_Line, out t);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds, m_Line, out var _) || entity == m_Ignore || !m_AircraftData.HasComponent(entity))
			{
				return;
			}
			Transform transform = m_TransformData[entity];
			if (!(transform.m_Position.y >= m_Line.a.y))
			{
				MathUtils.Distance(m_Line, transform.m_Position, out var t2);
				if (t2 < m_ClosestT)
				{
					m_Result = entity;
					m_ClosestT = t2;
				}
			}
		}
	}
}
