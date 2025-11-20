using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public static class HumanNavigationHelpers
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

	public struct CurrentLaneCache
	{
		private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		private Entity m_WasCurrentLane;

		private float m_WasCurvePosition;

		private bool m_WasEndReached;

		public CurrentLaneCache(ref HumanCurrentLane currentLane, EntityStorageInfoLookup entityLookup, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree)
		{
			if (!entityLookup.Exists(currentLane.m_Lane))
			{
				currentLane.m_Lane = Entity.Null;
			}
			m_SearchTree = searchTree;
			m_WasCurrentLane = currentLane.m_Lane;
			m_WasCurvePosition = currentLane.m_CurvePosition.x;
			m_WasEndReached = (currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0;
		}

		public void CheckChanges(Entity entity, ref HumanCurrentLane currentLane, LaneObjectCommandBuffer buffer, BufferLookup<LaneObject> laneObjects, Transform transform, Moving moving, HumanNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			QuadTreeBoundsXZ bounds;
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
					buffer.Add(currentLane.m_Lane, entity, currentLane.m_CurvePosition.xx);
				}
				else
				{
					buffer.Add(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
				}
			}
			else if (laneObjects.HasBuffer(m_WasCurrentLane))
			{
				if (!m_WasCurvePosition.Equals(currentLane.m_CurvePosition.x))
				{
					buffer.Update(m_WasCurrentLane, entity, currentLane.m_CurvePosition.xx);
				}
			}
			else if (m_SearchTree.TryGet(entity, out bounds))
			{
				Bounds3 bounds2 = CalculateMinBounds(transform, moving, navigation, objectGeometryData);
				if (math.any(bounds2.min < bounds.m_Bounds.min) | math.any(bounds2.max > bounds.m_Bounds.max) | (((currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0) & !m_WasEndReached))
				{
					buffer.Update(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
				}
			}
		}

		private Bounds3 CalculateMinBounds(Transform transform, Moving moving, HumanNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			float num = 4f / 15f;
			float3 x = moving.m_Velocity * num;
			float3 y = math.normalizesafe(navigation.m_TargetPosition - transform.m_Position) * (navigation.m_MaxSpeed * num);
			Bounds3 result = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData);
			result.min += math.min(0f, math.min(x, y));
			result.max += math.max(0f, math.max(x, y));
			return result;
		}

		private Bounds3 CalculateMaxBounds(Transform transform, Moving moving, HumanNavigation navigation, ObjectGeometryData objectGeometryData)
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

		public bool m_UnspawnedEmerge;

		public HumanCurrentLane m_Result;

		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		public BufferLookup<Triangle> m_AreaTriangles;

		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		public ComponentLookup<Curve> m_CurveData;

		public ComponentLookup<HangaroundLocation> m_HangaroundLocationData;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity ownerEntity)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_SubLanes.HasBuffer(ownerEntity))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[ownerEntity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				CreatureLaneFlags creatureLaneFlags = (CreatureLaneFlags)((uint)m_Result.m_Flags & 0xFFFFCFBFu);
				if (!m_PedestrianLaneData.HasComponent(subLane))
				{
					if (!m_ConnectionLaneData.HasComponent(subLane) || (m_ConnectionLaneData[subLane].m_Flags & ConnectionLaneFlags.Pedestrian) == 0)
					{
						continue;
					}
					creatureLaneFlags |= CreatureLaneFlags.Connection;
				}
				if (m_UnspawnedEmerge && (creatureLaneFlags & CreatureLaneFlags.Connection) == 0)
				{
					continue;
				}
				Bezier4x3 bezier = m_CurveData[subLane].m_Bezier;
				float num = MathUtils.Distance(MathUtils.Bounds(bezier), m_Position);
				if (num < m_MinDistance)
				{
					num = MathUtils.Distance(bezier, m_Position, out var t);
					if (num < m_MinDistance)
					{
						m_Bounds = new Bounds3(m_Position - num, m_Position + num);
						m_MinDistance = num;
						m_Result.m_Lane = subLane;
						m_Result.m_CurvePosition = t;
						m_Result.m_Flags = creatureLaneFlags;
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
			CreatureLaneFlags flags = (CreatureLaneFlags)0u;
			CreatureLaneFlags creatureLaneFlags = (CreatureLaneFlags)(((uint)m_Result.m_Flags & 0xFFFFDFBFu) | 0x1000);
			if (m_HangaroundLocationData.HasComponent(item.m_Area))
			{
				creatureLaneFlags |= CreatureLaneFlags.Hangaround;
			}
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (!m_ConnectionLaneData.HasComponent(subLane) || (m_ConnectionLaneData[subLane].m_Flags & ConnectionLaneFlags.Pedestrian) == 0)
				{
					continue;
				}
				Curve curve = m_CurveData[subLane];
				float2 t2;
				bool2 x = new bool2(MathUtils.Intersect(triangle2.xz, curve.m_Bezier.a.xz, out t2), MathUtils.Intersect(triangle2.xz, curve.m_Bezier.d.xz, out t2));
				if (math.any(x))
				{
					float t3;
					float num4 = MathUtils.Distance(curve.m_Bezier, position, out t3);
					if (num4 < num2)
					{
						float2 @float = math.select(new float2(0f, 0.49f), math.select(new float2(0.51f, 1f), new float2(0f, 1f), x.x), x.y);
						num2 = num4;
						entity = subLane;
						num3 = math.clamp(t3, @float.x, @float.y);
						flags = creatureLaneFlags;
					}
				}
			}
			if (entity != Entity.Null)
			{
				m_Bounds = new Bounds3(m_Position - num, m_Position + num);
				m_MinDistance = num;
				m_Result.m_Lane = entity;
				m_Result.m_CurvePosition = num3;
				m_Result.m_Flags = flags;
			}
		}
	}
}
