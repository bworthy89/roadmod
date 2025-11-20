using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public static class WatercraftNavigationHelpers
{
	public struct LaneSignal
	{
		public Entity m_Petitioner;

		public Entity m_Lane;

		public sbyte m_Priority;

		public LaneSignal(Entity petitioner, Entity lane, int priority)
		{
			m_Petitioner = petitioner;
			m_Lane = lane;
			m_Priority = (sbyte)priority;
		}
	}

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

		private Entity m_WasChangeLane;

		private float2 m_WasCurvePosition;

		public CurrentLaneCache(ref WatercraftCurrentLane currentLane, EntityStorageInfoLookup entityLookup, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree)
		{
			if (!entityLookup.Exists(currentLane.m_Lane))
			{
				currentLane.m_Lane = Entity.Null;
			}
			if (!entityLookup.Exists(currentLane.m_ChangeLane))
			{
				currentLane.m_ChangeLane = Entity.Null;
			}
			m_SearchTree = searchTree;
			m_WasCurrentLane = currentLane.m_Lane;
			m_WasChangeLane = currentLane.m_ChangeLane;
			m_WasCurvePosition = currentLane.m_CurvePosition.xy;
		}

		public void CheckChanges(Entity entity, ref WatercraftCurrentLane currentLane, LaneObjectCommandBuffer buffer, BufferLookup<LaneObject> laneObjects, Transform transform, Moving moving, WatercraftNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			if (currentLane.m_Lane == m_WasChangeLane)
			{
				QuadTreeBoundsXZ bounds;
				if (laneObjects.HasBuffer(m_WasCurrentLane))
				{
					buffer.Remove(m_WasCurrentLane, entity);
					if (laneObjects.HasBuffer(currentLane.m_Lane))
					{
						if (!m_WasCurvePosition.Equals(currentLane.m_CurvePosition.xy))
						{
							buffer.Update(currentLane.m_Lane, entity, currentLane.m_CurvePosition.xy);
						}
					}
					else
					{
						buffer.Add(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
					}
				}
				else if (laneObjects.HasBuffer(currentLane.m_Lane))
				{
					buffer.Remove(entity);
					if (!m_WasCurvePosition.Equals(currentLane.m_CurvePosition.xy))
					{
						buffer.Update(currentLane.m_Lane, entity, currentLane.m_CurvePosition.xy);
					}
				}
				else if (m_SearchTree.TryGet(entity, out bounds))
				{
					Bounds3 bounds2 = CalculateMinBounds(transform, moving, navigation, objectGeometryData);
					if (math.any(bounds2.min < bounds.m_Bounds.min) | math.any(bounds2.max > bounds.m_Bounds.max))
					{
						buffer.Update(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
					}
				}
				if (laneObjects.HasBuffer(currentLane.m_ChangeLane))
				{
					buffer.Add(currentLane.m_ChangeLane, entity, currentLane.m_CurvePosition.xy);
				}
				return;
			}
			QuadTreeBoundsXZ bounds3;
			if (currentLane.m_Lane != m_WasCurrentLane)
			{
				if (laneObjects.HasBuffer(m_WasCurrentLane))
				{
					buffer.Remove(m_WasCurrentLane, entity);
				}
				else
				{
					buffer.Remove(entity);
				}
				if (laneObjects.HasBuffer(currentLane.m_Lane))
				{
					buffer.Add(currentLane.m_Lane, entity, currentLane.m_CurvePosition.xy);
				}
				else
				{
					buffer.Add(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
				}
			}
			else if (laneObjects.HasBuffer(m_WasCurrentLane))
			{
				if (!m_WasCurvePosition.Equals(currentLane.m_CurvePosition.xy))
				{
					buffer.Update(m_WasCurrentLane, entity, currentLane.m_CurvePosition.xy);
				}
			}
			else if (m_SearchTree.TryGet(entity, out bounds3))
			{
				Bounds3 bounds4 = CalculateMinBounds(transform, moving, navigation, objectGeometryData);
				if (math.any(bounds4.min < bounds3.m_Bounds.min) | math.any(bounds4.max > bounds3.m_Bounds.max))
				{
					buffer.Update(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
				}
			}
			if (currentLane.m_ChangeLane != m_WasChangeLane)
			{
				if (laneObjects.HasBuffer(m_WasChangeLane))
				{
					buffer.Remove(m_WasChangeLane, entity);
				}
				if (laneObjects.HasBuffer(currentLane.m_ChangeLane))
				{
					buffer.Add(currentLane.m_ChangeLane, entity, currentLane.m_CurvePosition.xy);
				}
			}
			else if (laneObjects.HasBuffer(m_WasChangeLane) && !m_WasCurvePosition.Equals(currentLane.m_CurvePosition.xy))
			{
				buffer.Update(m_WasChangeLane, entity, currentLane.m_CurvePosition.xy);
			}
		}

		private Bounds3 CalculateMinBounds(Transform transform, Moving moving, WatercraftNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			float num = 4f / 15f;
			float3 x = moving.m_Velocity * num;
			float3 y = math.normalizesafe(navigation.m_TargetPosition - transform.m_Position) * (navigation.m_MaxSpeed * num);
			Bounds3 result = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData);
			result.min += math.min(0f, math.min(x, y));
			result.max += math.max(0f, math.max(x, y));
			return result;
		}

		private Bounds3 CalculateMaxBounds(Transform transform, Moving moving, WatercraftNavigation navigation, ObjectGeometryData objectGeometryData)
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

	public struct FindLaneIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
	{
		public Bounds3 m_Bounds;

		public float3 m_Position;

		public float m_MinDistance;

		public WatercraftCurrentLane m_Result;

		public RoadTypes m_CarType;

		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		public BufferLookup<Triangle> m_AreaTriangles;

		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		public ComponentLookup<MasterLane> m_MasterLaneData;

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
				WatercraftLaneFlags watercraftLaneFlags = m_Result.m_LaneFlags | WatercraftLaneFlags.FixedLane;
				watercraftLaneFlags = (WatercraftLaneFlags)((uint)watercraftLaneFlags & 0xFFFCFFFFu);
				if (!m_CarLaneData.HasComponent(subLane) || m_MasterLaneData.HasComponent(subLane))
				{
					continue;
				}
				if (m_CarLaneData.HasComponent(subLane))
				{
					if (m_MasterLaneData.HasComponent(subLane))
					{
						continue;
					}
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
					watercraftLaneFlags |= WatercraftLaneFlags.Connection;
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
						m_Result.m_LaneFlags = watercraftLaneFlags;
					}
				}
			}
		}

		public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_SubLanes.HasBuffer(item.m_Area))
			{
				return;
			}
			DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[item.m_Area];
			Triangle triangle = m_AreaTriangles[item.m_Area][item.m_Triangle];
			Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
			float2 t;
			float num = MathUtils.Distance(triangle2, m_Position, out t);
			if (num >= m_MinDistance)
			{
				return;
			}
			float3 position = MathUtils.Position(triangle2, t);
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[item.m_Area];
			float num2 = float.MaxValue;
			Entity entity = Entity.Null;
			float num3 = 0f;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (!m_ConnectionLaneData.HasComponent(subLane))
				{
					continue;
				}
				Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[subLane];
				if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) == 0 || (connectionLane.m_RoadTypes & m_CarType) == 0)
				{
					continue;
				}
				Curve curve = m_CurveData[subLane];
				if (MathUtils.Intersect(triangle2.xz, curve.m_Bezier.a.xz, out var t2) || MathUtils.Intersect(triangle2.xz, curve.m_Bezier.d.xz, out t2))
				{
					float t3;
					float num4 = MathUtils.Distance(curve.m_Bezier, position, out t3);
					if (num4 < num2)
					{
						num2 = num4;
						entity = subLane;
						num3 = t3;
					}
				}
			}
			if (entity != Entity.Null)
			{
				WatercraftLaneFlags watercraftLaneFlags = m_Result.m_LaneFlags | (WatercraftLaneFlags.FixedLane | WatercraftLaneFlags.Area);
				watercraftLaneFlags = (WatercraftLaneFlags)((uint)watercraftLaneFlags & 0xFFFEFFFFu);
				m_Bounds = new Bounds3(m_Position - num, m_Position + num);
				m_MinDistance = num;
				m_Result.m_Lane = entity;
				m_Result.m_CurvePosition = num3;
				m_Result.m_LaneFlags = watercraftLaneFlags;
			}
		}
	}
}
