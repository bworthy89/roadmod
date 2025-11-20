using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public static class TrainNavigationHelpers
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

		public byte m_Priority;

		public LaneReservation(Entity lane, int priority)
		{
			m_Lane = lane;
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
		private Entity m_WasCurrentLane1;

		private Entity m_WasCurrentLane2;

		private float2 m_WasCurvePosition1;

		private float2 m_WasCurvePosition2;

		public CurrentLaneCache(ref TrainCurrentLane currentLane, ComponentLookup<Lane> laneData)
		{
			if (currentLane.m_Front.m_Lane != Entity.Null && !laneData.HasComponent(currentLane.m_Front.m_Lane))
			{
				currentLane.m_Front.m_Lane = Entity.Null;
			}
			if (currentLane.m_Rear.m_Lane != Entity.Null && !laneData.HasComponent(currentLane.m_Rear.m_Lane))
			{
				currentLane.m_Rear.m_Lane = Entity.Null;
			}
			if (currentLane.m_FrontCache.m_Lane != Entity.Null && !laneData.HasComponent(currentLane.m_FrontCache.m_Lane))
			{
				currentLane.m_FrontCache.m_Lane = Entity.Null;
			}
			if (currentLane.m_RearCache.m_Lane != Entity.Null && !laneData.HasComponent(currentLane.m_RearCache.m_Lane))
			{
				currentLane.m_RearCache.m_Lane = Entity.Null;
			}
			m_WasCurrentLane1 = currentLane.m_Front.m_Lane;
			m_WasCurrentLane2 = currentLane.m_Rear.m_Lane;
			GetCurvePositions(ref currentLane, out m_WasCurvePosition1, out m_WasCurvePosition2);
		}

		public void CheckChanges(Entity entity, TrainCurrentLane currentLane, LaneObjectCommandBuffer buffer)
		{
			GetCurvePositions(ref currentLane, out var pos, out var pos2);
			if (currentLane.m_Rear.m_Lane == m_WasCurrentLane1)
			{
				if (currentLane.m_Front.m_Lane == m_WasCurrentLane2)
				{
					if (currentLane.m_Front.m_Lane != Entity.Null && !m_WasCurvePosition2.Equals(pos))
					{
						buffer.Update(currentLane.m_Front.m_Lane, entity, pos);
					}
					if (currentLane.m_Rear.m_Lane != currentLane.m_Front.m_Lane && currentLane.m_Rear.m_Lane != Entity.Null && !m_WasCurvePosition1.Equals(pos2))
					{
						buffer.Update(currentLane.m_Rear.m_Lane, entity, pos2);
					}
					return;
				}
				if (currentLane.m_Rear.m_Lane != m_WasCurrentLane2 && m_WasCurrentLane2 != Entity.Null)
				{
					buffer.Remove(m_WasCurrentLane2, entity);
				}
				if (currentLane.m_Rear.m_Lane != Entity.Null && !m_WasCurvePosition1.Equals(pos2))
				{
					buffer.Update(currentLane.m_Rear.m_Lane, entity, pos2);
				}
				if (currentLane.m_Front.m_Lane != m_WasCurrentLane1 && currentLane.m_Front.m_Lane != Entity.Null)
				{
					buffer.Add(currentLane.m_Front.m_Lane, entity, pos);
				}
				return;
			}
			if (currentLane.m_Front.m_Lane == m_WasCurrentLane2)
			{
				if (currentLane.m_Front.m_Lane != m_WasCurrentLane1 && m_WasCurrentLane1 != Entity.Null)
				{
					buffer.Remove(m_WasCurrentLane1, entity);
				}
				if (currentLane.m_Front.m_Lane != Entity.Null && !m_WasCurvePosition2.Equals(pos))
				{
					buffer.Update(currentLane.m_Front.m_Lane, entity, pos);
				}
				if (currentLane.m_Rear.m_Lane != m_WasCurrentLane2 && currentLane.m_Rear.m_Lane != Entity.Null)
				{
					buffer.Add(currentLane.m_Rear.m_Lane, entity, pos2);
				}
				return;
			}
			if (m_WasCurrentLane1 == m_WasCurrentLane2)
			{
				if (m_WasCurrentLane1 != Entity.Null)
				{
					buffer.Remove(m_WasCurrentLane1, entity);
				}
				if (currentLane.m_Front.m_Lane != Entity.Null)
				{
					buffer.Add(currentLane.m_Front.m_Lane, entity, pos);
				}
				if (currentLane.m_Rear.m_Lane != currentLane.m_Front.m_Lane && currentLane.m_Rear.m_Lane != Entity.Null)
				{
					buffer.Add(currentLane.m_Rear.m_Lane, entity, pos2);
				}
				return;
			}
			if (currentLane.m_Front.m_Lane == currentLane.m_Rear.m_Lane)
			{
				if (m_WasCurrentLane1 != Entity.Null)
				{
					buffer.Remove(m_WasCurrentLane1, entity);
				}
				if (m_WasCurrentLane2 != Entity.Null)
				{
					buffer.Remove(m_WasCurrentLane2, entity);
				}
				if (currentLane.m_Front.m_Lane != Entity.Null)
				{
					buffer.Add(currentLane.m_Front.m_Lane, entity, pos);
				}
				return;
			}
			if (currentLane.m_Front.m_Lane != m_WasCurrentLane1)
			{
				if (m_WasCurrentLane1 != Entity.Null)
				{
					buffer.Remove(m_WasCurrentLane1, entity);
				}
				if (currentLane.m_Front.m_Lane != Entity.Null)
				{
					buffer.Add(currentLane.m_Front.m_Lane, entity, pos);
				}
			}
			else if (m_WasCurrentLane1 != Entity.Null && !m_WasCurvePosition1.Equals(pos))
			{
				buffer.Update(m_WasCurrentLane1, entity, pos);
			}
			if (currentLane.m_Rear.m_Lane != m_WasCurrentLane2)
			{
				if (m_WasCurrentLane2 != Entity.Null)
				{
					buffer.Remove(m_WasCurrentLane2, entity);
				}
				if (currentLane.m_Rear.m_Lane != Entity.Null)
				{
					buffer.Add(currentLane.m_Rear.m_Lane, entity, pos2);
				}
			}
			else if (m_WasCurrentLane2 != Entity.Null && !m_WasCurvePosition2.Equals(pos2))
			{
				buffer.Update(m_WasCurrentLane2, entity, pos2);
			}
		}
	}

	public struct FindLaneIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Bounds3 m_Bounds;

		public float3 m_FrontPivot;

		public float3 m_RearPivot;

		public float2 m_MinDistance;

		public TrainCurrentLane m_Result;

		public TrackTypes m_TrackType;

		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

		public ComponentLookup<Curve> m_CurveData;

		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

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
			TrainLaneFlags trainLaneFlags = (TrainLaneFlags)((uint)m_Result.m_Front.m_LaneFlags & 0xFFFEFFFFu);
			TrainLaneFlags trainLaneFlags2 = (TrainLaneFlags)((uint)m_Result.m_Rear.m_LaneFlags & 0xFFFEFFFFu);
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[edgeEntity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (!m_TrackLaneData.HasComponent(subLane))
				{
					continue;
				}
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				if ((m_PrefabTrackLaneData[prefabRef.m_Prefab].m_TrackTypes & m_TrackType) == 0)
				{
					continue;
				}
				Bezier4x3 bezier = m_CurveData[subLane].m_Bezier;
				Bounds3 bounds2 = MathUtils.Bounds(bezier);
				float num = MathUtils.Distance(bounds2, m_FrontPivot);
				float num2 = MathUtils.Distance(bounds2, m_RearPivot);
				if (num < m_MinDistance.x)
				{
					num = MathUtils.Distance(bezier, m_FrontPivot, out var t);
					if (num < m_MinDistance.x)
					{
						TrainLaneFlags trainLaneFlags3 = trainLaneFlags;
						if (m_ConnectionLaneData.HasComponent(subLane))
						{
							trainLaneFlags3 |= TrainLaneFlags.Connection;
						}
						m_MinDistance.x = num;
						m_Result.m_Front = new TrainBogieLane(subLane, t, trainLaneFlags3);
						m_Result.m_FrontCache = new TrainBogieCache(m_Result.m_Front);
					}
				}
				if (!(num2 < m_MinDistance.y))
				{
					continue;
				}
				num2 = MathUtils.Distance(bezier, m_RearPivot, out var t2);
				if (num2 < m_MinDistance.y)
				{
					TrainLaneFlags trainLaneFlags4 = trainLaneFlags2;
					if (m_ConnectionLaneData.HasComponent(subLane))
					{
						trainLaneFlags4 |= TrainLaneFlags.Connection;
					}
					m_MinDistance.y = num2;
					m_Result.m_Rear = new TrainBogieLane(subLane, t2, trainLaneFlags4);
					m_Result.m_RearCache = new TrainBogieCache(m_Result.m_Rear);
				}
			}
			if (m_Result.m_Front.m_Lane != Entity.Null && m_Result.m_Rear.m_Lane == Entity.Null)
			{
				TrainLaneFlags laneFlags = trainLaneFlags2 | (m_Result.m_Front.m_LaneFlags & TrainLaneFlags.Connection);
				m_Result.m_Rear = new TrainBogieLane(m_Result.m_Front.m_Lane, m_Result.m_Front.m_CurvePosition, laneFlags);
				m_Result.m_RearCache = new TrainBogieCache(m_Result.m_Rear);
			}
			else if (m_Result.m_Front.m_Lane == Entity.Null && m_Result.m_Rear.m_Lane != Entity.Null)
			{
				TrainLaneFlags laneFlags2 = trainLaneFlags | (m_Result.m_Rear.m_LaneFlags & TrainLaneFlags.Connection);
				m_Result.m_Front = new TrainBogieLane(m_Result.m_Rear.m_Lane, m_Result.m_Rear.m_CurvePosition, laneFlags2);
				m_Result.m_FrontCache = new TrainBogieCache(m_Result.m_Front);
			}
		}
	}

	public static void GetCurvePositions(ref TrainCurrentLane currentLane, out float2 pos1, out float2 pos2)
	{
		pos1 = currentLane.m_Front.m_CurvePosition.yz;
		pos2 = currentLane.m_Rear.m_CurvePosition.yz;
		if (currentLane.m_Front.m_Lane == currentLane.m_Rear.m_Lane)
		{
			if (pos1.y < pos1.x)
			{
				pos1.y = math.min(pos1.y, pos2.y);
				pos1.x = math.max(pos1.x, pos2.x);
			}
			else
			{
				pos1.x = math.min(pos1.x, pos2.x);
				pos1.y = math.max(pos1.y, pos2.y);
			}
			pos2 = pos1;
		}
		else if (pos1.y < pos1.x)
		{
			pos1.x = math.max(pos1.x, currentLane.m_Front.m_CurvePosition.x);
		}
		else
		{
			pos1.x = math.min(pos1.x, currentLane.m_Front.m_CurvePosition.x);
		}
		if (currentLane.m_Rear.m_Lane != currentLane.m_RearCache.m_Lane)
		{
			if (pos2.y < pos2.x)
			{
				pos2.x = math.max(pos2.x, currentLane.m_Rear.m_CurvePosition.x);
			}
			else
			{
				pos2.x = math.min(pos2.x, currentLane.m_Rear.m_CurvePosition.x);
			}
		}
	}

	public static void GetCurvePositions(ref ParkedTrain parkedTrain, out float2 pos1, out float2 pos2)
	{
		pos1 = parkedTrain.m_CurvePosition.x;
		pos2 = parkedTrain.m_CurvePosition.y;
		if (parkedTrain.m_FrontLane == parkedTrain.m_RearLane)
		{
			pos1.x = math.min(pos1.x, pos2.x);
			pos1.y = math.max(pos1.y, pos2.y);
			pos2 = pos1;
		}
	}
}
