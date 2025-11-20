using Colossal.Mathematics;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct CreatureTargetIterator
{
	public ComponentLookup<Moving> m_MovingData;

	public ComponentLookup<Curve> m_CurveData;

	public ComponentLookup<LaneReservation> m_LaneReservationData;

	public BufferLookup<LaneOverlap> m_LaneOverlaps;

	public BufferLookup<LaneObject> m_LaneObjects;

	public ObjectGeometryData m_PrefabObjectGeometry;

	public Entity m_Blocker;

	public BlockerType m_BlockerType;

	public Entity m_QueueEntity;

	public Sphere3 m_QueueArea;

	private float m_TargetDelta;

	public bool IterateLane(Entity currentLane, ref float curveDelta, float targetDelta)
	{
		m_TargetDelta = targetDelta;
		if (m_LaneOverlaps.TryGetBuffer(currentLane, out var bufferData))
		{
			for (int i = 0; i < bufferData.Length; i++)
			{
				LaneOverlap laneOverlap = bufferData[i];
				if ((laneOverlap.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleStart | OverlapFlags.MergeMiddleEnd)) == 0 && (laneOverlap.m_Flags & (OverlapFlags.Road | OverlapFlags.Track)) != 0)
				{
					float4 @float = new float4((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd, (int)laneOverlap.m_OtherStart, (int)laneOverlap.m_OtherEnd) * 0.003921569f;
					if ((curveDelta <= @float.x) & (m_TargetDelta > @float.x))
					{
						CheckOverlapLane(currentLane, laneOverlap.m_Other, @float.x, targetDelta, @float.zw);
					}
					else if ((curveDelta >= @float.y) & (m_TargetDelta < @float.y))
					{
						CheckOverlapLane(currentLane, laneOverlap.m_Other, @float.y, targetDelta, @float.zw);
					}
				}
			}
		}
		curveDelta = m_TargetDelta;
		return m_TargetDelta == targetDelta;
	}

	private void CheckOverlapLane(Entity currentLane, Entity overlapLane, float limitDelta, float targetDelta, float2 overlapRange)
	{
		if (m_LaneReservationData.TryGetComponent(overlapLane, out var componentData))
		{
			float offset = componentData.GetOffset();
			int priority = componentData.GetPriority();
			if (offset > overlapRange.x || priority >= 108)
			{
				m_TargetDelta = limitDelta;
				m_Blocker = Entity.Null;
				m_BlockerType = BlockerType.Crossing;
				Curve curve = m_CurveData[currentLane];
				float3 position = MathUtils.Position(curve.m_Bezier, limitDelta);
				float3 position2 = math.select(curve.m_Bezier.a, curve.m_Bezier.d, targetDelta > limitDelta);
				m_QueueEntity = currentLane;
				m_QueueArea = CreatureUtils.GetQueueArea(m_PrefabObjectGeometry, position, position2);
				return;
			}
		}
		if (!m_LaneObjects.TryGetBuffer(overlapLane, out var bufferData))
		{
			return;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneObject laneObject = bufferData[i];
			float num = math.min(laneObject.m_CurvePosition.x, laneObject.m_CurvePosition.y);
			float num2 = math.max(laneObject.m_CurvePosition.x, laneObject.m_CurvePosition.y);
			if (((num <= overlapRange.y) & (num2 >= overlapRange.x)) && m_MovingData.HasComponent(laneObject.m_LaneObject))
			{
				m_TargetDelta = limitDelta;
				m_Blocker = laneObject.m_LaneObject;
				m_BlockerType = BlockerType.Crossing;
				Curve curve2 = m_CurveData[currentLane];
				float3 position3 = MathUtils.Position(curve2.m_Bezier, limitDelta);
				float3 position4 = math.select(curve2.m_Bezier.a, curve2.m_Bezier.d, targetDelta > limitDelta);
				m_QueueEntity = currentLane;
				m_QueueArea = CreatureUtils.GetQueueArea(m_PrefabObjectGeometry, position3, position4);
				break;
			}
		}
	}
}
