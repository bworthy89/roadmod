using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public static class CarNavigationHelpers
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

		public float2 m_Flow;

		public LaneEffects(Entity lane, float3 sideEffects, float2 flow)
		{
			m_Lane = lane;
			m_SideEffects = sideEffects;
			m_Flow = flow;
		}
	}

	public struct CurrentLaneCache
	{
		private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		private Entity m_WasCurrentLane;

		private Entity m_WasChangeLane;

		private float2 m_WasCurvePosition;

		private DynamicBuffer<BlockedLane> m_WasBlockedLanes;

		public CurrentLaneCache(ref CarCurrentLane currentLane, DynamicBuffer<BlockedLane> blockedLanes, EntityStorageInfoLookup entityLookup, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree)
		{
			if (!entityLookup.Exists(currentLane.m_Lane))
			{
				currentLane.m_Lane = Entity.Null;
			}
			if (currentLane.m_ChangeLane != Entity.Null && !entityLookup.Exists(currentLane.m_ChangeLane))
			{
				currentLane.m_ChangeLane = Entity.Null;
				currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
			}
			m_WasBlockedLanes = blockedLanes;
			m_SearchTree = searchTree;
			m_WasCurrentLane = currentLane.m_Lane;
			m_WasChangeLane = currentLane.m_ChangeLane;
			m_WasCurvePosition = currentLane.m_CurvePosition.xy;
		}

		public void CheckChanges(Entity entity, ref CarCurrentLane currentLane, NativeList<BlockedLane> newBlockedLanes, LaneObjectCommandBuffer buffer, BufferLookup<LaneObject> laneObjects, Transform transform, Moving moving, CarNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			if (newBlockedLanes.IsCreated && newBlockedLanes.Length != 0)
			{
				QuadTreeBoundsXZ bounds;
				if (laneObjects.HasBuffer(m_WasCurrentLane))
				{
					m_WasBlockedLanes.Add(new BlockedLane(m_WasCurrentLane, m_WasCurvePosition));
					buffer.Add(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
				}
				else if (m_SearchTree.TryGet(entity, out bounds))
				{
					Bounds3 bounds2 = CalculateMinBounds(transform, moving, navigation, objectGeometryData);
					if (math.any(bounds2.min < bounds.m_Bounds.min) | math.any(bounds2.max > bounds.m_Bounds.max))
					{
						buffer.Update(entity, CalculateMaxBounds(transform, moving, navigation, objectGeometryData));
					}
				}
				if (laneObjects.HasBuffer(m_WasChangeLane))
				{
					m_WasBlockedLanes.Add(new BlockedLane(m_WasChangeLane, m_WasCurvePosition));
				}
				int num = 0;
				while (num < m_WasBlockedLanes.Length)
				{
					BlockedLane blockedLane = m_WasBlockedLanes[num];
					int num2 = 0;
					while (true)
					{
						if (num2 < newBlockedLanes.Length)
						{
							BlockedLane value = newBlockedLanes[num2];
							if (value.m_Lane == blockedLane.m_Lane)
							{
								if (!value.m_CurvePosition.Equals(blockedLane.m_CurvePosition))
								{
									m_WasBlockedLanes[num] = value;
									buffer.Update(value.m_Lane, entity, value.m_CurvePosition);
								}
								newBlockedLanes.RemoveAtSwapBack(num2);
								num++;
								break;
							}
							num2++;
							continue;
						}
						m_WasBlockedLanes.RemoveAt(num);
						buffer.Remove(blockedLane.m_Lane, entity);
						break;
					}
				}
				for (int i = 0; i < newBlockedLanes.Length; i++)
				{
					BlockedLane elem = newBlockedLanes[i];
					m_WasBlockedLanes.Add(elem);
					buffer.Add(elem.m_Lane, entity, elem.m_CurvePosition);
				}
				return;
			}
			if (m_WasBlockedLanes.Length != 0)
			{
				for (int j = 0; j < m_WasBlockedLanes.Length; j++)
				{
					Entity lane = m_WasBlockedLanes[j].m_Lane;
					if (laneObjects.HasBuffer(lane))
					{
						buffer.Remove(lane, entity);
					}
				}
				m_WasBlockedLanes.Clear();
				m_WasCurrentLane = Entity.Null;
				m_WasChangeLane = Entity.Null;
			}
			if (currentLane.m_Lane == m_WasChangeLane)
			{
				QuadTreeBoundsXZ bounds3;
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
				else if (m_SearchTree.TryGet(entity, out bounds3))
				{
					Bounds3 bounds4 = CalculateMinBounds(transform, moving, navigation, objectGeometryData);
					if (math.any(bounds4.min < bounds3.m_Bounds.min) | math.any(bounds4.max > bounds3.m_Bounds.max))
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
			QuadTreeBoundsXZ bounds5;
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
			else if (m_SearchTree.TryGet(entity, out bounds5))
			{
				Bounds3 bounds6 = CalculateMinBounds(transform, moving, navigation, objectGeometryData);
				if (math.any(bounds6.min < bounds5.m_Bounds.min) | math.any(bounds6.max > bounds5.m_Bounds.max))
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

		private Bounds3 CalculateMinBounds(Transform transform, Moving moving, CarNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			float num = 4f / 15f;
			float3 x = moving.m_Velocity * num;
			float3 y = math.normalizesafe(navigation.m_TargetPosition - transform.m_Position) * math.abs(navigation.m_MaxSpeed * num);
			Bounds3 result = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData);
			result.min += math.min(0f, math.min(x, y));
			result.max += math.max(0f, math.max(x, y));
			return result;
		}

		private Bounds3 CalculateMaxBounds(Transform transform, Moving moving, CarNavigation navigation, ObjectGeometryData objectGeometryData)
		{
			float num = -1.0666667f;
			float num2 = 2f;
			float num3 = math.length(objectGeometryData.m_Size) * 0.5f;
			float3 x = moving.m_Velocity * num;
			float3 x2 = moving.m_Velocity * num2;
			float3 y = math.normalizesafe(navigation.m_TargetPosition - transform.m_Position) * math.abs(navigation.m_MaxSpeed * num2);
			float3 position = transform.m_Position;
			position.y += objectGeometryData.m_Size.y * 0.5f;
			Bounds3 result = default(Bounds3);
			result.min = position - num3 + math.min(x, math.min(x2, y));
			result.max = position + num3 + math.max(x, math.max(x2, y));
			return result;
		}
	}

	public struct TrailerLaneCache
	{
		private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		private Entity m_WasCurrentLane;

		private Entity m_WasNextLane;

		private float2 m_WasCurrentPosition;

		private float2 m_WasNextPosition;

		private DynamicBuffer<BlockedLane> m_WasBlockedLanes;

		public TrailerLaneCache(ref CarTrailerLane trailerLane, DynamicBuffer<BlockedLane> blockedLanes, ComponentLookup<PrefabRef> prefabRefData, NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree)
		{
			if (!prefabRefData.HasComponent(trailerLane.m_Lane))
			{
				trailerLane.m_Lane = Entity.Null;
			}
			if (!prefabRefData.HasComponent(trailerLane.m_NextLane))
			{
				trailerLane.m_NextLane = Entity.Null;
			}
			m_WasBlockedLanes = blockedLanes;
			m_SearchTree = searchTree;
			m_WasCurrentLane = trailerLane.m_Lane;
			m_WasNextLane = trailerLane.m_NextLane;
			m_WasCurrentPosition = trailerLane.m_CurvePosition;
			m_WasNextPosition = trailerLane.m_NextPosition;
		}

		public void CheckChanges(Entity entity, ref CarTrailerLane trailerLane, NativeList<BlockedLane> newBlockedLanes, LaneObjectCommandBuffer buffer, BufferLookup<LaneObject> laneObjects, Transform transform, Moving moving, CarNavigation tractorNavigation, ObjectGeometryData objectGeometryData)
		{
			if (newBlockedLanes.IsCreated && newBlockedLanes.Length != 0)
			{
				QuadTreeBoundsXZ bounds;
				if (laneObjects.HasBuffer(m_WasCurrentLane))
				{
					m_WasBlockedLanes.Add(new BlockedLane(m_WasCurrentLane, m_WasCurrentPosition));
					buffer.Add(entity, CalculateMaxBounds(transform, moving, tractorNavigation, objectGeometryData));
				}
				else if (m_SearchTree.TryGet(entity, out bounds))
				{
					Bounds3 bounds2 = CalculateMinBounds(transform, moving, tractorNavigation, objectGeometryData);
					if (math.any(bounds2.min < bounds.m_Bounds.min) | math.any(bounds2.max > bounds.m_Bounds.max))
					{
						buffer.Update(entity, CalculateMaxBounds(transform, moving, tractorNavigation, objectGeometryData));
					}
				}
				if (laneObjects.HasBuffer(m_WasNextLane))
				{
					m_WasBlockedLanes.Add(new BlockedLane(m_WasNextLane, m_WasNextPosition));
				}
				int num = 0;
				while (num < m_WasBlockedLanes.Length)
				{
					BlockedLane blockedLane = m_WasBlockedLanes[num];
					int num2 = 0;
					while (true)
					{
						if (num2 < newBlockedLanes.Length)
						{
							BlockedLane value = newBlockedLanes[num2];
							if (value.m_Lane == blockedLane.m_Lane)
							{
								if (!value.m_CurvePosition.Equals(blockedLane.m_CurvePosition))
								{
									m_WasBlockedLanes[num] = value;
									buffer.Update(value.m_Lane, entity, value.m_CurvePosition);
								}
								newBlockedLanes.RemoveAtSwapBack(num2);
								num++;
								break;
							}
							num2++;
							continue;
						}
						m_WasBlockedLanes.RemoveAt(num);
						buffer.Remove(blockedLane.m_Lane, entity);
						break;
					}
				}
				for (int i = 0; i < newBlockedLanes.Length; i++)
				{
					BlockedLane elem = newBlockedLanes[i];
					m_WasBlockedLanes.Add(elem);
					buffer.Add(elem.m_Lane, entity, elem.m_CurvePosition);
				}
				return;
			}
			if (m_WasBlockedLanes.Length != 0)
			{
				for (int j = 0; j < m_WasBlockedLanes.Length; j++)
				{
					Entity lane = m_WasBlockedLanes[j].m_Lane;
					if (laneObjects.HasBuffer(lane))
					{
						buffer.Remove(lane, entity);
					}
				}
				m_WasBlockedLanes.Clear();
				m_WasCurrentLane = Entity.Null;
				m_WasNextLane = Entity.Null;
			}
			if (trailerLane.m_Lane == m_WasNextLane)
			{
				QuadTreeBoundsXZ bounds3;
				if (laneObjects.HasBuffer(m_WasCurrentLane))
				{
					buffer.Remove(m_WasCurrentLane, entity);
					if (laneObjects.HasBuffer(trailerLane.m_Lane))
					{
						if (!m_WasNextPosition.Equals(trailerLane.m_CurvePosition))
						{
							buffer.Update(trailerLane.m_Lane, entity, trailerLane.m_CurvePosition);
						}
					}
					else
					{
						buffer.Add(entity, CalculateMaxBounds(transform, moving, tractorNavigation, objectGeometryData));
					}
				}
				else if (laneObjects.HasBuffer(trailerLane.m_Lane))
				{
					buffer.Remove(entity);
					if (!m_WasNextPosition.Equals(trailerLane.m_CurvePosition))
					{
						buffer.Update(trailerLane.m_Lane, entity, trailerLane.m_CurvePosition);
					}
				}
				else if (m_SearchTree.TryGet(entity, out bounds3))
				{
					Bounds3 bounds4 = CalculateMinBounds(transform, moving, tractorNavigation, objectGeometryData);
					if (math.any(bounds4.min < bounds3.m_Bounds.min) | math.any(bounds4.max > bounds3.m_Bounds.max))
					{
						buffer.Update(entity, CalculateMaxBounds(transform, moving, tractorNavigation, objectGeometryData));
					}
				}
				if (laneObjects.HasBuffer(trailerLane.m_NextLane))
				{
					buffer.Add(trailerLane.m_NextLane, entity, trailerLane.m_NextPosition);
				}
				return;
			}
			QuadTreeBoundsXZ bounds5;
			if (trailerLane.m_Lane != m_WasCurrentLane)
			{
				if (laneObjects.HasBuffer(m_WasCurrentLane))
				{
					buffer.Remove(m_WasCurrentLane, entity);
				}
				else
				{
					buffer.Remove(entity);
				}
				if (laneObjects.HasBuffer(trailerLane.m_Lane))
				{
					buffer.Add(trailerLane.m_Lane, entity, trailerLane.m_CurvePosition);
				}
				else
				{
					buffer.Add(entity, CalculateMaxBounds(transform, moving, tractorNavigation, objectGeometryData));
				}
			}
			else if (laneObjects.HasBuffer(m_WasCurrentLane))
			{
				if (!m_WasCurrentPosition.Equals(trailerLane.m_CurvePosition))
				{
					buffer.Update(m_WasCurrentLane, entity, trailerLane.m_CurvePosition);
				}
			}
			else if (m_SearchTree.TryGet(entity, out bounds5))
			{
				Bounds3 bounds6 = CalculateMinBounds(transform, moving, tractorNavigation, objectGeometryData);
				if (math.any(bounds6.min < bounds5.m_Bounds.min) | math.any(bounds6.max > bounds5.m_Bounds.max))
				{
					buffer.Update(entity, CalculateMaxBounds(transform, moving, tractorNavigation, objectGeometryData));
				}
			}
			if (trailerLane.m_NextLane != m_WasNextLane)
			{
				if (laneObjects.HasBuffer(m_WasNextLane))
				{
					buffer.Remove(m_WasNextLane, entity);
				}
				if (laneObjects.HasBuffer(trailerLane.m_NextLane))
				{
					buffer.Add(trailerLane.m_NextLane, entity, trailerLane.m_NextPosition);
				}
			}
			else if (laneObjects.HasBuffer(m_WasNextLane) && !m_WasNextPosition.Equals(trailerLane.m_NextPosition))
			{
				buffer.Update(m_WasNextLane, entity, trailerLane.m_NextPosition);
			}
		}

		private Bounds3 CalculateMinBounds(Transform transform, Moving moving, CarNavigation tractorNavigation, ObjectGeometryData objectGeometryData)
		{
			float num = 4f / 15f;
			float3 x = moving.m_Velocity * num;
			float3 y = math.normalizesafe(tractorNavigation.m_TargetPosition - transform.m_Position) * math.abs(tractorNavigation.m_MaxSpeed * num);
			Bounds3 result = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData);
			result.min += math.min(0f, math.min(x, y));
			result.max += math.max(0f, math.max(x, y));
			return result;
		}

		private Bounds3 CalculateMaxBounds(Transform transform, Moving moving, CarNavigation tractorNavigation, ObjectGeometryData objectGeometryData)
		{
			float num = -1.0666667f;
			float num2 = 2f;
			float num3 = math.length(objectGeometryData.m_Size) * 0.5f;
			float3 x = moving.m_Velocity * num;
			float3 x2 = moving.m_Velocity * num2;
			float3 y = math.normalizesafe(tractorNavigation.m_TargetPosition - transform.m_Position) * math.abs(tractorNavigation.m_MaxSpeed * num2);
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

		public CarCurrentLane m_Result;

		public RoadTypes m_CarType;

		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		public BufferLookup<Triangle> m_AreaTriangles;

		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		public ComponentLookup<MasterLane> m_MasterLaneData;

		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		public ComponentLookup<Curve> m_CurveData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

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
				Game.Vehicles.CarLaneFlags carLaneFlags = m_Result.m_LaneFlags | (Game.Vehicles.CarLaneFlags.EnteringRoad | Game.Vehicles.CarLaneFlags.FixedLane);
				carLaneFlags = (Game.Vehicles.CarLaneFlags)((uint)carLaneFlags & 0xE2FFFFEFu);
				Game.Net.ConnectionLane componentData2;
				Game.Net.PedestrianLane componentData3;
				if (m_CarLaneData.TryGetComponent(subLane, out var componentData))
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
					if ((componentData.m_Flags & Game.Net.CarLaneFlags.Twoway) != 0 && m_CarType != RoadTypes.Bicycle)
					{
						carLaneFlags |= Game.Vehicles.CarLaneFlags.CanReverse;
					}
					if ((componentData.m_Flags & (Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout)) == Game.Net.CarLaneFlags.Roundabout)
					{
						carLaneFlags |= Game.Vehicles.CarLaneFlags.Roundabout;
					}
				}
				else if (m_ConnectionLaneData.TryGetComponent(subLane, out componentData2))
				{
					if ((m_CarType != RoadTypes.Bicycle || (componentData2.m_Flags & ConnectionLaneFlags.Pedestrian) == 0 || (componentData2.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) == 0) && ((componentData2.m_Flags & ConnectionLaneFlags.Road) == 0 || (componentData2.m_RoadTypes & m_CarType) == 0))
					{
						continue;
					}
					carLaneFlags |= Game.Vehicles.CarLaneFlags.Connection;
				}
				else if (m_CarType != RoadTypes.Bicycle || !m_PedestrianLaneData.TryGetComponent(subLane, out componentData3) || (componentData3.m_Flags & PedestrianLaneFlags.AllowBicycle) == 0)
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
						m_Result.m_LaneFlags = carLaneFlags;
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
				if (!m_ConnectionLaneData.TryGetComponent(subLane, out var componentData) || ((m_CarType != RoadTypes.Bicycle || (componentData.m_Flags & ConnectionLaneFlags.Pedestrian) == 0 || (componentData.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) == 0) && ((componentData.m_Flags & ConnectionLaneFlags.Road) == 0 || (componentData.m_RoadTypes & m_CarType) == 0)))
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
				Game.Vehicles.CarLaneFlags carLaneFlags = m_Result.m_LaneFlags | (Game.Vehicles.CarLaneFlags.EnteringRoad | Game.Vehicles.CarLaneFlags.FixedLane | Game.Vehicles.CarLaneFlags.Area);
				carLaneFlags = (Game.Vehicles.CarLaneFlags)((uint)carLaneFlags & 0xE6FFFFEFu);
				m_Bounds = new Bounds3(m_Position - num, m_Position + num);
				m_MinDistance = num;
				m_Result.m_Lane = entity;
				m_Result.m_CurvePosition = num3;
				m_Result.m_LaneFlags = carLaneFlags;
			}
		}
	}

	public struct FindBlockedLanesIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Bounds3 m_Bounds;

		public Line3.Segment m_Line;

		public float m_Radius;

		public NativeList<BlockedLane> m_BlockedLanes;

		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public ComponentLookup<MasterLane> m_MasterLaneData;

		public ComponentLookup<Curve> m_CurveData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<NetLaneData> m_PrefabLaneData;

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
				if (m_MasterLaneData.HasComponent(subLane))
				{
					continue;
				}
				Entity prefab = m_PrefabRefData[subLane].m_Prefab;
				Bezier4x3 bezier = m_CurveData[subLane].m_Bezier;
				NetLaneData netLaneData = m_PrefabLaneData[prefab];
				float num = m_Radius + netLaneData.m_Width * 0.4f;
				if (MathUtils.Intersect(MathUtils.Expand(MathUtils.Bounds(bezier), num), m_Line, out var _))
				{
					float num2 = MathUtils.Distance(bezier, m_Line, out var t2);
					if (num2 < num)
					{
						num2 = math.max(0f, num2 - netLaneData.m_Width * 0.4f);
						float length = math.sqrt(math.max(0f, m_Radius * m_Radius - num2 * num2)) + netLaneData.m_Width * 0.4f;
						Bounds1 t3 = new Bounds1(0f, t2.x);
						Bounds1 t4 = new Bounds1(t2.x, 1f);
						MathUtils.ClampLengthInverse(bezier, ref t3, length);
						MathUtils.ClampLength(bezier, ref t4, length);
						m_BlockedLanes.Add(new BlockedLane(subLane, new float2(t3.min, t4.max)));
					}
				}
			}
		}
	}
}
