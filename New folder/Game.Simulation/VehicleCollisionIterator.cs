using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct VehicleCollisionIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
{
	public ComponentLookup<Owner> m_OwnerData;

	public ComponentLookup<Transform> m_TransformData;

	public ComponentLookup<Moving> m_MovingData;

	public ComponentLookup<Controller> m_ControllerData;

	public ComponentLookup<Creature> m_CreatureData;

	public ComponentLookup<Curve> m_CurveData;

	public ComponentLookup<AreaLane> m_AreaLaneData;

	public ComponentLookup<PrefabRef> m_PrefabRefData;

	public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

	public ComponentLookup<NetLaneData> m_PrefabLaneData;

	public BufferLookup<Game.Areas.Node> m_AreaNodes;

	public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

	public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

	public TerrainHeightData m_TerrainHeightData;

	public Entity m_Entity;

	public Entity m_CurrentLane;

	public float m_CurvePosition;

	public float m_TimeStep;

	public ObjectGeometryData m_PrefabObjectGeometry;

	public Bounds1 m_SpeedRange;

	public float3 m_CurrentPosition;

	public float3 m_CurrentVelocity;

	public float m_MinDistance;

	public float3 m_TargetPosition;

	public float m_MaxSpeed;

	public float m_LanePosition;

	public Entity m_Blocker;

	public BlockerType m_BlockerType;

	private Line3.Segment m_TargetLine;

	private Bounds1 m_TargetLimits;

	private float m_PushFactor;

	private Bounds3 m_Bounds;

	private float m_Size;

	public bool IterateFirstLane(Entity currentLane)
	{
		m_Size = (m_PrefabObjectGeometry.m_Bounds.max.x - m_PrefabObjectGeometry.m_Bounds.min.x) * 0.5f;
		m_PushFactor = 0.75f;
		if (m_AreaLaneData.HasComponent(currentLane))
		{
			CalculateTargetLine(currentLane, m_TargetPosition);
			m_MovingObjectSearchTree.Iterate(ref this);
			m_StaticObjectSearchTree.Iterate(ref this);
			return false;
		}
		return false;
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

	private void CalculateTargetLine(Entity targetLane, float3 targetPosition)
	{
		Owner owner = m_OwnerData[targetLane];
		AreaLane areaLane = m_AreaLaneData[targetLane];
		DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_AreaNodes[owner.m_Owner];
		float3 @float = targetPosition - m_CurrentPosition;
		float num = math.length(@float.xz);
		if (num < m_MinDistance)
		{
			m_TargetLine = new Line3.Segment(targetPosition, targetPosition);
			m_TargetLimits = new Bounds1(0f, 1f);
		}
		else
		{
			if (num > m_MinDistance)
			{
				targetPosition = m_CurrentPosition + @float * (m_MinDistance / num);
			}
			float2 float2 = MathUtils.Right(@float.xz) * (0.5f / math.max(0.1f, num));
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
			m_TargetLimits.min = math.min(bounds.min + m_Size, 0f);
			m_TargetLimits.max = math.max(bounds.max - m_Size, 0f);
			bounds.min = math.max(m_TargetLimits.min, m_MinDistance * -0.9f);
			bounds.max = math.min(m_TargetLimits.max, m_MinDistance * 0.9f);
			m_TargetLine.a = MathUtils.Position(line, bounds.min);
			m_TargetLine.b = MathUtils.Position(line, bounds.max);
			float num2 = 1f / math.max(1f, m_TargetLimits.max - m_TargetLimits.min);
			m_TargetLimits.min = (bounds.min - m_TargetLimits.min) * num2;
			m_TargetLimits.max = (bounds.max - m_TargetLimits.min) * num2;
		}
		m_Bounds = MathUtils.Expand(MathUtils.Bounds(m_TargetLine) | m_CurrentPosition, m_Size);
	}

	private void CheckCollision(Entity other)
	{
		if (other == m_Entity || !m_TransformData.TryGetComponent(other, out var componentData))
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
			if (m_CreatureData.HasComponent(other) || (m_ControllerData.TryGetComponent(other, out var componentData3) && componentData3.m_Controller == m_Entity))
			{
				return;
			}
			float num = (objectGeometryData.m_Bounds.max.x - objectGeometryData.m_Bounds.min.x) * 0.5f;
			float num2 = m_Size + num + 0.5f;
			Line2.Segment segment = new Line2.Segment(m_CurrentPosition.xz, m_TargetPosition.xz);
			Line2.Segment segment2 = new Line2.Segment(componentData.m_Position.xz, componentData.m_Position.xz + componentData2.m_Velocity.xz);
			if (math.dot(segment2.a + componentData2.m_Velocity.xz * (m_TimeStep * 2f) - segment.a - m_CurrentVelocity.xz * m_TimeStep, m_TargetPosition.xz - m_CurrentPosition.xz) < 0f)
			{
				return;
			}
			float2 t;
			float num3 = MathUtils.Distance(segment, segment2, out t);
			if (!(num3 < num2))
			{
				return;
			}
			float2 @float = MathUtils.Position(segment, t.x * 0.99f);
			float2 float2 = MathUtils.Position(segment2, t.y);
			float2 float3 = math.normalizesafe(m_TargetPosition.xz - m_CurrentPosition.xz);
			float2 x = @float - float2;
			x -= float3 * math.dot(x, float3);
			x = math.normalizesafe(x);
			float2 position = m_TargetPosition.xz + x * ((num2 - num3) * m_PushFactor);
			m_PushFactor /= 2f;
			if (m_TargetLine.a.Equals(m_TargetLine.b))
			{
				m_TargetPosition = m_TargetLine.a;
			}
			else
			{
				MathUtils.Distance(m_TargetLine.xz, position, out m_LanePosition);
				m_TargetPosition = MathUtils.Position(m_TargetLine, m_LanePosition);
				m_LanePosition = m_TargetLimits.min + m_LanePosition * (m_TargetLimits.max - m_TargetLimits.min) - 0.5f;
			}
			if (m_TerrainHeightData.isCreated)
			{
				m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, m_TargetPosition);
			}
			float num4 = math.min(1f, 0.7f + 0.3f * math.dot(float3, math.normalizesafe(componentData2.m_Velocity.xz)) + num3 / num2);
			num4 *= m_SpeedRange.max;
			x = componentData.m_Position.xz - m_CurrentPosition.xz;
			float num5 = math.length(x);
			float num6 = math.dot(x, float3);
			BlockerType blockerType = BlockerType.Crossing;
			if (num5 < num2 && num6 > 0f)
			{
				blockerType = BlockerType.Continuing;
				if (num5 > 0.01f)
				{
					float num7 = num6 * (num2 - num5) / (num2 * num5);
					num4 = math.min(num4, math.max(0f, math.max(1f, math.lerp(math.dot(float3, componentData2.m_Velocity.xz), m_SpeedRange.max, num3 / num2)) - num7));
				}
				else
				{
					num4 = 0f;
				}
			}
			num4 = MathUtils.Clamp(num4, m_SpeedRange);
			if (num4 < m_MaxSpeed)
			{
				m_MaxSpeed = num4;
				m_Blocker = other;
				m_BlockerType = blockerType;
			}
			return;
		}
		float num8 = (((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) == 0) ? (math.cmax(objectGeometryData.m_Bounds.max.xz - objectGeometryData.m_Bounds.min.xz) * 0.5f) : math.cmax(objectGeometryData.m_LegSize.xz + objectGeometryData.m_LegOffset * 2f));
		float num9 = m_Size + num8 + 0.25f;
		Line2.Segment line = new Line2.Segment(m_CurrentPosition.xz, m_TargetPosition.xz);
		float t2;
		float num10 = MathUtils.Distance(line, componentData.m_Position.xz, out t2);
		if (num10 < num9)
		{
			float2 float4 = MathUtils.Position(line, t2 * 0.99f);
			float2 float5 = math.normalizesafe(m_TargetPosition.xz - m_CurrentPosition.xz);
			float2 x2 = float4 - componentData.m_Position.xz;
			x2 -= float5 * math.dot(x2, float5);
			x2 = math.normalizesafe(x2);
			float2 position2 = m_TargetPosition.xz + x2 * ((num9 - num10) * m_PushFactor);
			m_PushFactor /= 2f;
			if (m_TargetLine.a.Equals(m_TargetLine.b))
			{
				m_TargetPosition = m_TargetLine.a;
			}
			else
			{
				MathUtils.Distance(m_TargetLine.xz, position2, out m_LanePosition);
				m_TargetPosition = MathUtils.Position(m_TargetLine, m_LanePosition);
				m_LanePosition = m_TargetLimits.min + m_LanePosition * (m_TargetLimits.max - m_TargetLimits.min) - 0.5f;
			}
			if (m_TerrainHeightData.isCreated)
			{
				m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, m_TargetPosition);
			}
		}
	}
}
