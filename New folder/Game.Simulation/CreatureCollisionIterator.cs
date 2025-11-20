using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct CreatureCollisionIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
{
	public ComponentLookup<Owner> m_OwnerData;

	public ComponentLookup<Transform> m_TransformData;

	public ComponentLookup<Moving> m_MovingData;

	public ComponentLookup<Creature> m_CreatureData;

	public ComponentLookup<GroupMember> m_GroupMemberData;

	public ComponentLookup<Waypoint> m_WaypointData;

	public ComponentLookup<TaxiStand> m_TaxiStandData;

	public ComponentLookup<Curve> m_CurveData;

	public ComponentLookup<AreaLane> m_AreaLaneData;

	public ComponentLookup<NodeLane> m_NodeLaneData;

	public ComponentLookup<PrefabRef> m_PrefabRefData;

	public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

	public ComponentLookup<NetLaneData> m_PrefabLaneData;

	public BufferLookup<LaneObject> m_LaneObjects;

	public BufferLookup<Game.Areas.Node> m_AreaNodes;

	public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

	public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

	public Entity m_Entity;

	public Entity m_Leader;

	public Entity m_CurrentLane;

	public Entity m_CurrentVehicle;

	public float m_CurvePosition;

	public float m_TimeStep;

	public ObjectGeometryData m_PrefabObjectGeometry;

	public Bounds1 m_SpeedRange;

	public float3 m_CurrentPosition;

	public float3 m_CurrentDirection;

	public float3 m_CurrentVelocity;

	public float m_TargetDistance;

	public PathOwner m_PathOwner;

	public DynamicBuffer<PathElement> m_PathElements;

	public float m_MinSpeed;

	public float3 m_TargetPosition;

	public float m_MaxSpeed;

	public float m_LanePosition;

	public Entity m_Blocker;

	public BlockerType m_BlockerType;

	public Entity m_QueueEntity;

	public Sphere3 m_QueueArea;

	public DynamicBuffer<Queue> m_Queues;

	private Line3.Segment m_TargetLine;

	private float m_PushFactor;

	private Bounds3 m_Bounds;

	private float m_Size;

	public bool IterateFirstLane(Entity currentLane, float2 currentOffset, bool isBackward)
	{
		return IterateFirstLane(currentLane, currentLane, currentOffset, currentOffset, isBackward);
	}

	public bool IterateFirstLane(Entity currentLane, Entity targetLane, float2 currentOffset, float2 targetOffset, bool isBackward)
	{
		m_Size = (m_PrefabObjectGeometry.m_Bounds.max.x - m_PrefabObjectGeometry.m_Bounds.min.x) * 0.5f;
		m_PushFactor = 0.75f;
		if (m_AreaLaneData.HasComponent(targetLane))
		{
			CalculateTargetLine(targetLane, m_TargetPosition, isBackward);
			m_MovingObjectSearchTree.Iterate(ref this);
			m_StaticObjectSearchTree.Iterate(ref this);
			return false;
		}
		if (m_CurveData.TryGetComponent(targetLane, out var componentData))
		{
			CalculateTargetLine(targetLane, targetOffset.x);
			bool result = false;
			if (m_LaneObjects.TryGetBuffer(currentLane, out var bufferData))
			{
				float num = m_TargetDistance / math.max(1f, componentData.m_Length);
				Bounds1 bounds = new Bounds1(currentOffset.x - num, currentOffset.x + num);
				result = MathUtils.Intersect(bounds, currentOffset.y);
				for (int i = 0; i < bufferData.Length; i++)
				{
					LaneObject laneObject = bufferData[i];
					Bounds1 bounds2 = MathUtils.Bounds(laneObject.m_CurvePosition.x, laneObject.m_CurvePosition.y);
					if (MathUtils.Intersect(bounds, bounds2) && laneObject.m_LaneObject != m_Entity)
					{
						CheckCollision(laneObject.m_LaneObject);
					}
				}
			}
			m_StaticObjectSearchTree.Iterate(ref this);
			return result;
		}
		return false;
	}

	public bool IterateNextLane(Entity nextLane, float2 nextOffset)
	{
		bool result = false;
		if (m_LaneObjects.TryGetBuffer(nextLane, out var bufferData))
		{
			float num = 5f / math.max(1f, m_CurveData[nextLane].m_Length);
			Bounds1 bounds = new Bounds1(nextOffset.x - num, nextOffset.x + num);
			result = MathUtils.Intersect(bounds, nextOffset.y);
			for (int i = 0; i < bufferData.Length; i++)
			{
				LaneObject laneObject = bufferData[i];
				Bounds1 bounds2 = MathUtils.Bounds(laneObject.m_CurvePosition.x, laneObject.m_CurvePosition.y);
				if (MathUtils.Intersect(bounds, bounds2) && laneObject.m_LaneObject != m_Entity)
				{
					CheckCollision(laneObject.m_LaneObject);
				}
			}
		}
		else if (m_AreaLaneData.HasComponent(nextLane))
		{
			m_MovingObjectSearchTree.Iterate(ref this);
		}
		return result;
	}

	public bool Intersect(QuadTreeBoundsXZ bounds)
	{
		if ((bounds.m_Mask & (BoundsMask.NotOverridden | BoundsMask.NotWalkThrough)) != (BoundsMask.NotOverridden | BoundsMask.NotWalkThrough))
		{
			return false;
		}
		return MathUtils.Intersect(m_Bounds, bounds.m_Bounds);
	}

	public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
	{
		if ((bounds.m_Mask & (BoundsMask.NotOverridden | BoundsMask.NotWalkThrough)) == (BoundsMask.NotOverridden | BoundsMask.NotWalkThrough) && MathUtils.Intersect(m_Bounds, bounds.m_Bounds))
		{
			CheckCollision(item);
		}
	}

	private void CalculateTargetLine(Entity targetLane, float3 targetPosition, bool isBackward)
	{
		Owner owner = m_OwnerData[targetLane];
		AreaLane areaLane = m_AreaLaneData[targetLane];
		DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_AreaNodes[owner.m_Owner];
		float3 @float = targetPosition - m_CurrentPosition;
		float num = math.length(@float.xz);
		if (num < m_TargetDistance)
		{
			m_TargetLine = new Line3.Segment(targetPosition, targetPosition);
		}
		else
		{
			if (num > m_TargetDistance)
			{
				targetPosition = m_CurrentPosition + @float * (m_TargetDistance / num);
			}
			float2 float2 = math.select(MathUtils.Right(@float.xz), MathUtils.Left(@float.xz), isBackward) * (0.5f / math.max(0.1f, num));
			Line3 line = new Line3.Segment(targetPosition, targetPosition);
			line.a.xz -= float2;
			line.b.xz += float2;
			Bounds1 bounds = default(Bounds1);
			float2 t;
			if (areaLane.m_Nodes.y == areaLane.m_Nodes.z)
			{
				float3 position = dynamicBuffer[areaLane.m_Nodes.x].m_Position;
				float3 position2 = dynamicBuffer[areaLane.m_Nodes.y].m_Position;
				float3 position3 = dynamicBuffer[areaLane.m_Nodes.w].m_Position;
				if (MathUtils.Intersect(new Line2.Segment(position.xz, position2.xz), line.xz, out t))
				{
					bounds |= t.y;
				}
				if (MathUtils.Intersect(new Line2.Segment(position2.xz, position3.xz), line.xz, out t))
				{
					bounds |= t.y;
				}
				if (MathUtils.Intersect(new Line2.Segment(position3.xz, position.xz), line.xz, out t))
				{
					bounds |= t.y;
				}
			}
			else
			{
				float3 position4 = dynamicBuffer[areaLane.m_Nodes.x].m_Position;
				float3 position5 = dynamicBuffer[areaLane.m_Nodes.y].m_Position;
				float3 position6 = dynamicBuffer[areaLane.m_Nodes.w].m_Position;
				float3 position7 = dynamicBuffer[areaLane.m_Nodes.z].m_Position;
				if (MathUtils.Intersect(new Line2.Segment(position4.xz, position5.xz), line.xz, out t))
				{
					bounds |= t.y;
				}
				if (MathUtils.Intersect(new Line2.Segment(position5.xz, position6.xz), line.xz, out t))
				{
					bounds |= t.y;
				}
				if (MathUtils.Intersect(new Line2.Segment(position6.xz, position7.xz), line.xz, out t))
				{
					bounds |= t.y;
				}
				if (MathUtils.Intersect(new Line2.Segment(position7.xz, position4.xz), line.xz, out t))
				{
					bounds |= t.y;
				}
			}
			bounds.min = math.clamp(bounds.min + m_Size, m_TargetDistance * -0.9f, 0f);
			bounds.max = math.clamp(bounds.max - m_Size, 0f, m_TargetDistance * 0.9f);
			m_TargetLine.a = MathUtils.Position(line, bounds.min);
			m_TargetLine.b = MathUtils.Position(line, bounds.max);
		}
		m_Bounds = MathUtils.Expand(MathUtils.Bounds(m_TargetLine) | m_CurrentPosition, m_Size);
	}

	private void CalculateTargetLine(Entity targetLane, float targetOffset)
	{
		Curve curve = m_CurveData[targetLane];
		PrefabRef prefabRef = m_PrefabRefData[targetLane];
		NetLaneData netLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
		m_NodeLaneData.TryGetComponent(targetLane, out var componentData);
		float num = netLaneData.m_Width + math.lerp(componentData.m_WidthOffset.x, componentData.m_WidthOffset.y, targetOffset);
		float num2 = math.max(0f, num * 0.5f - m_Size);
		float3 @float = MathUtils.Position(curve.m_Bezier, targetOffset);
		float2 float2 = MathUtils.Right(math.normalizesafe(MathUtils.Tangent(curve.m_Bezier, targetOffset).xz)) * num2;
		m_TargetLine = new Line3.Segment(@float, @float);
		m_TargetLine.a.xz -= float2;
		m_TargetLine.b.xz += float2;
		float t;
		float num3 = MathUtils.Distance(m_TargetLine, m_CurrentPosition, out t);
		if (num3 > m_TargetDistance)
		{
			m_TargetLine += (m_CurrentPosition - MathUtils.Position(m_TargetLine, t)) * (1f - m_TargetDistance / num3);
		}
		m_Bounds = MathUtils.Expand(MathUtils.Bounds(m_TargetLine) | m_CurrentPosition, m_Size);
	}

	private void CheckCollision(Entity other)
	{
		if (!m_TransformData.TryGetComponent(other, out var componentData))
		{
			return;
		}
		PrefabRef prefabRef = m_PrefabRefData[other];
		ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
		if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.WalkThrough) != Game.Objects.GeometryFlags.None)
		{
			return;
		}
		if (m_MovingData.TryGetComponent(other, out var componentData2))
		{
			float num = (objectGeometryData.m_Bounds.max.x - objectGeometryData.m_Bounds.min.x) * 0.5f;
			float num2 = m_Size + num + 0.5f;
			Line3.Segment segment = new Line3.Segment(m_CurrentPosition, m_TargetPosition);
			Line3.Segment segment2 = new Line3.Segment(componentData.m_Position, componentData.m_Position + componentData2.m_Velocity);
			if (math.dot(segment2.a + componentData2.m_Velocity * (m_TimeStep * 2f) - segment.a - m_CurrentVelocity * m_TimeStep, m_TargetPosition - m_CurrentPosition) < 0f)
			{
				return;
			}
			float2 t;
			float num3 = MathUtils.Distance(segment, segment2, out t);
			if (!(num3 < num2))
			{
				return;
			}
			float3 @float = MathUtils.Position(segment, t.x * 0.99f);
			float3 float2 = MathUtils.Position(segment2, t.y);
			Bounds1 bounds = @float.y + m_PrefabObjectGeometry.m_Bounds.y;
			Bounds1 bounds2 = float2.y + objectGeometryData.m_Bounds.y;
			if (!MathUtils.Intersect(bounds, bounds2))
			{
				return;
			}
			float3 float3 = math.normalizesafe(m_TargetPosition - m_CurrentPosition);
			float3 x = @float - float2;
			x -= float3 * math.dot(x, float3);
			x = math.normalizesafe(x);
			float3 position = m_TargetPosition + x * ((num2 - num3) * m_PushFactor);
			m_PushFactor /= 2f;
			if (m_TargetLine.a.Equals(m_TargetLine.b))
			{
				m_TargetPosition = m_TargetLine.a;
			}
			else
			{
				MathUtils.Distance(m_TargetLine, position, out m_LanePosition);
				m_TargetPosition = MathUtils.Position(m_TargetLine, m_LanePosition);
				m_LanePosition -= 0.5f;
			}
			float num4 = math.min(1f, 0.7f + 0.3f * math.dot(float3, math.normalizesafe(componentData2.m_Velocity)) + num3 / num2);
			num4 *= m_SpeedRange.max;
			x = componentData.m_Position - m_CurrentPosition;
			float num5 = math.length(x);
			float num6 = math.dot(x, float3);
			Entity queueEntity = Entity.Null;
			Sphere3 queueArea = default(Sphere3);
			BlockerType blockerType = BlockerType.Crossing;
			if (num5 < num2 && num6 > 0f)
			{
				blockerType = BlockerType.Continuing;
				if (CheckQueue(other, out queueEntity, out queueArea))
				{
					if (num5 > 0.01f)
					{
						float num7 = num6 * (num2 - num5) / (num2 * num5);
						num4 = math.min(num4, math.max(0f, math.max(1f, math.lerp(math.dot(float3, componentData2.m_Velocity), m_SpeedRange.max, num3 / num2)) - num7));
					}
					else
					{
						num4 = 0f;
					}
				}
				else if (num5 > 0.01f && ((objectGeometryData.m_Flags & ~m_PrefabObjectGeometry.m_Flags & Game.Objects.GeometryFlags.LowCollisionPriority) == 0 || math.dot(componentData2.m_Velocity, x) < 0f))
				{
					float num8 = num6 * (num2 - num5) / (num2 * num5);
					num4 = math.min(num4, math.max(m_MinSpeed, math.max(1f, math.lerp(math.dot(float3, componentData2.m_Velocity), m_SpeedRange.max, num3 / num2)) - num8));
				}
			}
			num4 = MathUtils.Clamp(num4, m_SpeedRange);
			if (num4 < m_MaxSpeed)
			{
				m_MaxSpeed = num4;
				m_Blocker = other;
				m_BlockerType = blockerType;
				CreatureUtils.SetQueue(ref m_QueueEntity, ref m_QueueArea, queueEntity, queueArea);
			}
			return;
		}
		float num9 = (((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) == 0) ? (math.cmax(objectGeometryData.m_Bounds.max.xz - objectGeometryData.m_Bounds.min.xz) * 0.5f) : math.cmax(objectGeometryData.m_LegSize.xz + objectGeometryData.m_LegOffset * 2f));
		float num10 = m_Size + num9 + 0.25f;
		Line3.Segment line = new Line3.Segment(m_CurrentPosition, m_TargetPosition);
		float t2;
		float num11 = MathUtils.Distance(line, componentData.m_Position, out t2);
		if (!(num11 < num10))
		{
			return;
		}
		float3 float4 = MathUtils.Position(line, t2 * 0.99f);
		Bounds1 bounds3 = float4.y + m_PrefabObjectGeometry.m_Bounds.y;
		Bounds1 bounds4 = componentData.m_Position.y + objectGeometryData.m_Bounds.y;
		if (MathUtils.Intersect(bounds3, bounds4))
		{
			float3 float5 = math.normalizesafe(m_TargetPosition - m_CurrentPosition);
			float3 x2 = float4 - componentData.m_Position;
			x2 -= float5 * math.dot(x2, float5);
			x2 = math.normalizesafe(x2);
			float3 position2 = m_TargetPosition + x2 * ((num10 - num11) * m_PushFactor);
			m_PushFactor /= 2f;
			if (m_TargetLine.a.Equals(m_TargetLine.b))
			{
				m_TargetPosition = m_TargetLine.a;
			}
			else
			{
				MathUtils.Distance(m_TargetLine, position2, out m_LanePosition);
				m_TargetPosition = MathUtils.Position(m_TargetLine, m_LanePosition);
				m_LanePosition -= 0.5f;
			}
			float num12 = math.min(1f, 0.7f + num11 / num10);
			num12 *= m_SpeedRange.max;
			x2 = componentData.m_Position - m_CurrentPosition;
			float num13 = math.length(x2);
			float num14 = math.dot(x2, float5);
			if (num13 < num10 && num14 > 0f && num13 > 0.01f)
			{
				float num15 = num14 * (num10 - num13) / (num10 * num13);
				num12 = math.min(num12, math.max(0.5f, math.max(1f, m_SpeedRange.max * num11 / num10) - num15));
			}
			num12 = MathUtils.Clamp(num12, m_SpeedRange);
			if (num12 < m_MaxSpeed)
			{
				m_MaxSpeed = num12;
				m_Blocker = other;
				m_BlockerType = BlockerType.Limit;
				m_QueueEntity = Entity.Null;
				m_QueueArea = default(Sphere3);
			}
		}
	}

	private bool CheckQueue(Entity other, out Entity queueEntity, out Sphere3 queueArea)
	{
		queueEntity = Entity.Null;
		queueArea = default(Sphere3);
		if (m_CreatureData.TryGetComponent(other, out var componentData) && componentData.m_QueueArea.radius > 0f)
		{
			Transform transform = m_TransformData[other];
			float3 y = math.forward(transform.m_Rotation);
			if (math.dot(transform.m_Position - m_CurrentPosition, m_CurrentDirection) < math.dot(m_CurrentPosition - transform.m_Position, y))
			{
				return false;
			}
			if (m_Leader != Entity.Null)
			{
				if (m_GroupMemberData.TryGetComponent(other, out var componentData2))
				{
					other = componentData2.m_Leader;
				}
				if (other == m_Leader)
				{
					queueEntity = componentData.m_QueueEntity;
					queueArea = componentData.m_QueueArea;
					return true;
				}
			}
			else
			{
				if (m_GroupMemberData.TryGetComponent(other, out var componentData3))
				{
					other = componentData3.m_Leader;
				}
				if (other != m_Entity && ShouldQueue(componentData.m_QueueEntity, componentData.m_QueueArea, out queueArea))
				{
					queueEntity = componentData.m_QueueEntity;
					return true;
				}
			}
		}
		return false;
	}

	private bool ShouldQueue(Entity entity, Sphere3 area, out Sphere3 queueArea)
	{
		if (!m_Queues.IsCreated || (m_PathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0)
		{
			queueArea = default(Sphere3);
			return false;
		}
		Entity entity2 = Entity.Null;
		if (m_PathElements.Length > m_PathOwner.m_ElementIndex)
		{
			PathElement pathElement = m_PathElements[m_PathOwner.m_ElementIndex];
			if (m_WaypointData.HasComponent(pathElement.m_Target) || m_TaxiStandData.HasComponent(pathElement.m_Target))
			{
				entity2 = pathElement.m_Target;
			}
		}
		for (int i = 0; i < m_Queues.Length; i++)
		{
			Queue value = m_Queues[i];
			if (value.m_TargetEntity == entity)
			{
				if ((value.m_TargetEntity == entity2 || value.m_TargetEntity == m_CurrentLane) && m_CurveData.TryGetComponent(m_CurrentLane, out var componentData))
				{
					PrefabRef prefabRef = m_PrefabRefData[m_CurrentLane];
					NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
					m_NodeLaneData.TryGetComponent(m_CurrentLane, out var componentData2);
					float laneOffset = CreatureUtils.GetLaneOffset(m_PrefabObjectGeometry, prefabLaneData, componentData2, m_CurvePosition, m_LanePosition);
					value.m_TargetArea.position = CreatureUtils.GetLanePosition(componentData.m_Bezier, m_CurvePosition, laneOffset);
				}
				value.m_ObsoleteTime = 0;
				m_Queues[i] = value;
				if (value.m_TargetArea.radius > 0f && MathUtils.Intersect(value.m_TargetArea, area))
				{
					Sphere3 queueArea2 = CreatureUtils.GetQueueArea(m_PrefabObjectGeometry, m_CurrentPosition, m_TargetPosition);
					queueArea = MathUtils.Sphere(area, MathUtils.Sphere(queueArea2, value.m_TargetArea));
					return true;
				}
				queueArea = default(Sphere3);
				return false;
			}
		}
		if (m_CurrentLane == entity)
		{
			Queue elem = default(Queue);
			elem.m_TargetEntity = entity;
			elem.m_TargetArea = CreatureUtils.GetQueueArea(m_PrefabObjectGeometry, GetTargetPosition(m_PathOwner.m_ElementIndex - 1, m_CurrentLane, m_CurvePosition));
			elem.m_ObsoleteTime = 0;
			m_Queues.Add(elem);
			if (MathUtils.Intersect(elem.m_TargetArea, area))
			{
				Sphere3 queueArea3 = CreatureUtils.GetQueueArea(m_PrefabObjectGeometry, m_CurrentPosition, m_TargetPosition);
				queueArea = MathUtils.Sphere(area, MathUtils.Sphere(queueArea3, elem.m_TargetArea));
				return true;
			}
			queueArea = default(Sphere3);
			return false;
		}
		if (m_CurrentVehicle == Entity.Null)
		{
			Queue elem2 = default(Queue);
			for (int j = m_PathOwner.m_ElementIndex; j < m_PathElements.Length; j++)
			{
				PathElement pathElement2 = m_PathElements[j];
				if (pathElement2.m_Target == entity)
				{
					elem2.m_TargetEntity = entity;
					elem2.m_TargetArea = CreatureUtils.GetQueueArea(m_PrefabObjectGeometry, GetTargetPosition(j, pathElement2.m_Target, pathElement2.m_TargetDelta.y));
					elem2.m_ObsoleteTime = 0;
					m_Queues.Add(elem2);
					if (MathUtils.Intersect(elem2.m_TargetArea, area))
					{
						Sphere3 queueArea4 = CreatureUtils.GetQueueArea(m_PrefabObjectGeometry, m_CurrentPosition, m_TargetPosition);
						queueArea = MathUtils.Sphere(area, MathUtils.Sphere(queueArea4, elem2.m_TargetArea));
						return true;
					}
					queueArea = default(Sphere3);
					return false;
				}
			}
		}
		m_Queues.Add(new Queue
		{
			m_TargetEntity = entity
		});
		queueArea = default(Sphere3);
		return false;
	}

	private float3 GetTargetPosition(int elementIndex, Entity targetElement, float curvePos)
	{
		while (m_WaypointData.HasComponent(targetElement) || m_TaxiStandData.HasComponent(targetElement))
		{
			if (--elementIndex >= m_PathOwner.m_ElementIndex)
			{
				PathElement pathElement = m_PathElements[elementIndex];
				targetElement = pathElement.m_Target;
				curvePos = pathElement.m_TargetDelta.y;
				continue;
			}
			targetElement = m_CurrentLane;
			curvePos = m_CurvePosition;
			break;
		}
		if (m_CurveData.TryGetComponent(targetElement, out var componentData))
		{
			PrefabRef prefabRef = m_PrefabRefData[targetElement];
			NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
			m_NodeLaneData.TryGetComponent(targetElement, out var componentData2);
			float laneOffset = CreatureUtils.GetLaneOffset(m_PrefabObjectGeometry, prefabLaneData, componentData2, curvePos, m_LanePosition);
			return CreatureUtils.GetLanePosition(componentData.m_Bezier, curvePos, laneOffset);
		}
		if (m_TransformData.TryGetComponent(targetElement, out var componentData3))
		{
			return componentData3.m_Position;
		}
		return m_TargetPosition;
	}

	public void IterateBlocker(HumanData prefabHumanData, Entity other)
	{
		if (CheckQueue(other, out var queueEntity, out var queueArea) && m_MovingData.TryGetComponent(other, out var componentData))
		{
			Transform transform = m_TransformData[other];
			PrefabRef prefabRef = m_PrefabRefData[other];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			float num = (m_PrefabObjectGeometry.m_Bounds.max.x - m_PrefabObjectGeometry.m_Bounds.min.x) * 0.5f;
			float num2 = (objectGeometryData.m_Bounds.max.x - objectGeometryData.m_Bounds.min.x) * 0.5f;
			float num3 = num + num2 + 0.5f;
			float3 x = transform.m_Position - m_CurrentPosition;
			float3 @float = math.normalizesafe(m_TargetPosition - m_CurrentPosition);
			float distance = math.max(0f, math.length(x) * 2f - num3 - math.dot(x, @float));
			float maxResultSpeed = math.max(0f, math.dot(@float, componentData.m_Velocity));
			float maxBrakingSpeed = CreatureUtils.GetMaxBrakingSpeed(prefabHumanData, distance, maxResultSpeed, m_TimeStep);
			maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			if (maxBrakingSpeed <= m_MaxSpeed)
			{
				m_MaxSpeed = maxBrakingSpeed;
				m_Blocker = other;
				m_BlockerType = BlockerType.Continuing;
				CreatureUtils.SetQueue(ref m_QueueEntity, ref m_QueueArea, queueEntity, queueArea);
			}
		}
	}

	public void IterateBlocker(AnimalData prefabAnimalData, Entity other)
	{
		if (CheckQueue(other, out var queueEntity, out var queueArea) && m_MovingData.TryGetComponent(other, out var componentData))
		{
			Transform transform = m_TransformData[other];
			PrefabRef prefabRef = m_PrefabRefData[other];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			float num = (m_PrefabObjectGeometry.m_Bounds.max.x - m_PrefabObjectGeometry.m_Bounds.min.x) * 0.5f;
			float num2 = (objectGeometryData.m_Bounds.max.x - objectGeometryData.m_Bounds.min.x) * 0.5f;
			float num3 = num + num2 + 0.5f;
			float3 x = transform.m_Position - m_CurrentPosition;
			float3 @float = math.normalizesafe(m_TargetPosition - m_CurrentPosition);
			float distance = math.max(0f, math.length(x) * 2f - num3 - math.dot(x, @float));
			float maxResultSpeed = math.max(0f, math.dot(@float, componentData.m_Velocity));
			float maxBrakingSpeed = CreatureUtils.GetMaxBrakingSpeed(prefabAnimalData, distance, maxResultSpeed, m_TimeStep);
			maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			if (maxBrakingSpeed <= m_MaxSpeed)
			{
				m_MaxSpeed = maxBrakingSpeed;
				m_Blocker = other;
				m_BlockerType = BlockerType.Continuing;
				CreatureUtils.SetQueue(ref m_QueueEntity, ref m_QueueArea, queueEntity, queueArea);
			}
		}
	}
}
