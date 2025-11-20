using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation;

public struct WatercraftLaneSelectIterator
{
	public ComponentLookup<Owner> m_OwnerData;

	public ComponentLookup<Lane> m_LaneData;

	public ComponentLookup<SlaveLane> m_SlaveLaneData;

	public ComponentLookup<LaneReservation> m_LaneReservationData;

	public ComponentLookup<Moving> m_MovingData;

	public ComponentLookup<Watercraft> m_WatercraftData;

	public BufferLookup<SubLane> m_Lanes;

	public BufferLookup<LaneObject> m_LaneObjects;

	public Entity m_Entity;

	public Entity m_Blocker;

	public int m_Priority;

	private NativeArray<float> m_Buffer;

	private int m_BufferPos;

	private float m_LaneSwitchCost;

	private float m_LaneSwitchBaseCost;

	private Entity m_PrevLane;

	public void SetBuffer(ref WatercraftLaneSelectBuffer buffer)
	{
		m_Buffer = buffer.Ensure();
	}

	public void CalculateLaneCosts(WatercraftNavigationLane navLaneData, int index)
	{
		if ((navLaneData.m_Flags & (WatercraftLaneFlags.Reserved | WatercraftLaneFlags.FixedLane)) == 0 && m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
		{
			SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
			Owner owner = m_OwnerData[navLaneData.m_Lane];
			DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
			int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
			float laneObjectCost = math.abs(navLaneData.m_CurvePosition.y - navLaneData.m_CurvePosition.x) * 0.49f;
			for (int i = slaveLane.m_MinIndex; i <= num; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				float value = CalculateLaneObjectCost(laneObjectCost, index, subLane, navLaneData.m_Flags);
				m_Buffer[m_BufferPos++] = value;
			}
		}
	}

	private float CalculateLaneObjectCost(float laneObjectCost, int index, Entity lane, WatercraftLaneFlags laneFlags)
	{
		float num = 0f;
		if (m_LaneObjects.HasBuffer(lane))
		{
			DynamicBuffer<LaneObject> dynamicBuffer = m_LaneObjects[lane];
			if (index < 2 && m_Blocker != Entity.Null)
			{
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					LaneObject laneObject = dynamicBuffer[i];
					num = ((!(laneObject.m_LaneObject == m_Blocker)) ? (num + laneObjectCost) : (num + CalculateLaneObjectCost(laneObject, laneObjectCost, laneFlags)));
				}
			}
			else
			{
				num += laneObjectCost * (float)dynamicBuffer.Length;
			}
		}
		return num;
	}

	private float CalculateLaneObjectCost(float laneObjectCost, Entity lane, float minCurvePosition, WatercraftLaneFlags laneFlags)
	{
		float num = 0f;
		if (m_LaneObjects.HasBuffer(lane))
		{
			DynamicBuffer<LaneObject> dynamicBuffer = m_LaneObjects[lane];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				LaneObject laneObject = dynamicBuffer[i];
				if (laneObject.m_CurvePosition.y > minCurvePosition)
				{
					num += CalculateLaneObjectCost(laneObject, laneObjectCost, laneFlags);
				}
			}
		}
		return num;
	}

	private float CalculateLaneObjectCost(LaneObject laneObject, float laneObjectCost, WatercraftLaneFlags laneFlags)
	{
		if (!m_MovingData.HasComponent(laneObject.m_LaneObject))
		{
			if (m_WatercraftData.HasComponent(laneObject.m_LaneObject) && (m_WatercraftData[laneObject.m_LaneObject].m_Flags & WatercraftFlags.Queueing) != 0 && (laneFlags & WatercraftLaneFlags.Queue) != 0)
			{
				return laneObjectCost;
			}
			return math.lerp(10000000f, 9000000f, laneObject.m_CurvePosition.y);
		}
		return laneObjectCost;
	}

	public void CalculateLaneCosts(WatercraftNavigationLane navLaneData, WatercraftNavigationLane nextNavLaneData, int index)
	{
		if ((navLaneData.m_Flags & (WatercraftLaneFlags.Reserved | WatercraftLaneFlags.FixedLane)) == 0 && m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
		{
			SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
			Owner owner = m_OwnerData[navLaneData.m_Lane];
			DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
			int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
			m_LaneSwitchCost = m_LaneSwitchBaseCost + math.select(1f, 5f, (slaveLane.m_Flags & SlaveLaneFlags.AllowChange) == 0);
			float laneObjectCost = math.abs(navLaneData.m_CurvePosition.y - navLaneData.m_CurvePosition.x) * 0.49f;
			if ((nextNavLaneData.m_Flags & (WatercraftLaneFlags.Reserved | WatercraftLaneFlags.FixedLane)) == 0 && m_SlaveLaneData.HasComponent(nextNavLaneData.m_Lane))
			{
				SlaveLane slaveLane2 = m_SlaveLaneData[nextNavLaneData.m_Lane];
				Owner owner2 = m_OwnerData[nextNavLaneData.m_Lane];
				DynamicBuffer<SubLane> dynamicBuffer2 = m_Lanes[owner2.m_Owner];
				int num2 = math.min(slaveLane2.m_MaxIndex, dynamicBuffer2.Length - 1);
				int num3 = m_BufferPos - (num2 - slaveLane2.m_MinIndex + 1);
				for (int i = slaveLane.m_MinIndex; i <= num; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					Lane lane = m_LaneData[subLane];
					float num4 = 1000000f;
					int num5;
					int num6;
					if ((nextNavLaneData.m_Flags & WatercraftLaneFlags.GroupTarget) != 0)
					{
						num5 = slaveLane2.m_MinIndex;
						num6 = num2;
					}
					else
					{
						num5 = 100000;
						num6 = -100000;
						for (int j = slaveLane2.m_MinIndex; j <= num2; j++)
						{
							Lane lane2 = m_LaneData[dynamicBuffer2[j].m_SubLane];
							if (lane.m_EndNode.Equals(lane2.m_StartNode))
							{
								num5 = math.min(num5, j);
								num6 = j;
							}
						}
					}
					if (num5 <= num6)
					{
						int num7 = num3;
						for (int k = slaveLane2.m_MinIndex; k < num5; k++)
						{
							num4 = math.min(num4, m_Buffer[num7++] + GetLaneSwitchCost(num5 - k));
						}
						for (int l = num5; l <= num6; l++)
						{
							num4 = math.min(num4, m_Buffer[num7++]);
						}
						for (int m = num6 + 1; m <= num2; m++)
						{
							num4 = math.min(num4, m_Buffer[num7++] + GetLaneSwitchCost(m - num6));
						}
						num4 += CalculateLaneObjectCost(laneObjectCost, index, subLane, navLaneData.m_Flags);
						if (m_LaneReservationData.HasComponent(subLane))
						{
							num4 += GetLanePriorityCost(m_LaneReservationData[subLane].GetPriority());
						}
					}
					m_Buffer[m_BufferPos++] = num4;
				}
			}
			else if ((nextNavLaneData.m_Flags & WatercraftLaneFlags.TransformTarget) != 0)
			{
				for (int n = slaveLane.m_MinIndex; n <= num; n++)
				{
					Entity subLane2 = dynamicBuffer[n].m_SubLane;
					float num8 = CalculateLaneObjectCost(laneObjectCost, index, subLane2, navLaneData.m_Flags);
					if (m_LaneReservationData.HasComponent(subLane2))
					{
						num8 += GetLanePriorityCost(m_LaneReservationData[subLane2].GetPriority());
					}
					m_Buffer[m_BufferPos++] = num8;
				}
			}
			else
			{
				int num9 = 100000;
				int num10 = -100000;
				if ((nextNavLaneData.m_Flags & WatercraftLaneFlags.GroupTarget) != 0)
				{
					for (int num11 = slaveLane.m_MinIndex; num11 <= num; num11++)
					{
						if (dynamicBuffer[num11].m_SubLane == nextNavLaneData.m_Lane)
						{
							num9 = num11;
							num10 = num11;
							break;
						}
					}
				}
				else
				{
					Lane lane3 = m_LaneData[nextNavLaneData.m_Lane];
					for (int num12 = slaveLane.m_MinIndex; num12 <= num; num12++)
					{
						if (m_LaneData[dynamicBuffer[num12].m_SubLane].m_EndNode.Equals(lane3.m_StartNode))
						{
							num9 = math.min(num9, num12);
							num10 = num12;
						}
					}
				}
				for (int num13 = slaveLane.m_MinIndex; num13 <= num; num13++)
				{
					Entity subLane3 = dynamicBuffer[num13].m_SubLane;
					float num14 = 0f;
					if (num9 <= num10)
					{
						num14 += GetLaneSwitchCost(math.max(0, math.max(num9 - num13, num13 - num10)));
					}
					num14 += CalculateLaneObjectCost(laneObjectCost, index, subLane3, navLaneData.m_Flags);
					if (m_LaneReservationData.HasComponent(subLane3))
					{
						num14 += GetLanePriorityCost(m_LaneReservationData[subLane3].GetPriority());
					}
					m_Buffer[m_BufferPos++] = num14;
				}
			}
		}
		m_LaneSwitchBaseCost += 0.01f;
	}

	private float GetLaneSwitchCost(int numLanes)
	{
		return (float)(numLanes * numLanes * numLanes) * m_LaneSwitchCost;
	}

	private float GetLanePriorityCost(int lanePriority)
	{
		return (float)math.max(0, lanePriority - m_Priority) * 1f;
	}

	public void UpdateOptimalLane(ref WatercraftCurrentLane currentLaneData, WatercraftNavigationLane nextNavLaneData)
	{
		Entity entity = currentLaneData.m_Lane;
		if ((currentLaneData.m_LaneFlags & WatercraftLaneFlags.FixedLane) == 0 && m_SlaveLaneData.HasComponent(currentLaneData.m_Lane))
		{
			SlaveLane slaveLane = m_SlaveLaneData[currentLaneData.m_Lane];
			Owner owner = m_OwnerData[currentLaneData.m_Lane];
			DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
			int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
			m_LaneSwitchCost = m_LaneSwitchBaseCost + math.select(1f, 5f, (slaveLane.m_Flags & SlaveLaneFlags.AllowChange) == 0);
			float laneObjectCost = 0.49f;
			int num2 = 0;
			for (int i = slaveLane.m_MinIndex; i <= num; i++)
			{
				if (dynamicBuffer[i].m_SubLane == currentLaneData.m_Lane)
				{
					num2 = i;
					break;
				}
			}
			float num3 = float.MaxValue;
			if ((nextNavLaneData.m_Flags & (WatercraftLaneFlags.Reserved | WatercraftLaneFlags.FixedLane)) == 0 && m_SlaveLaneData.HasComponent(nextNavLaneData.m_Lane))
			{
				SlaveLane slaveLane2 = m_SlaveLaneData[nextNavLaneData.m_Lane];
				Owner owner2 = m_OwnerData[nextNavLaneData.m_Lane];
				DynamicBuffer<SubLane> dynamicBuffer2 = m_Lanes[owner2.m_Owner];
				int num4 = math.min(slaveLane2.m_MaxIndex, dynamicBuffer2.Length - 1);
				int num5 = m_BufferPos - (num4 - slaveLane2.m_MinIndex + 1);
				for (int j = slaveLane.m_MinIndex; j <= num; j++)
				{
					Entity subLane = dynamicBuffer[j].m_SubLane;
					Lane lane = m_LaneData[subLane];
					float num6 = 1000000f;
					int num7;
					int num8;
					if ((nextNavLaneData.m_Flags & WatercraftLaneFlags.GroupTarget) != 0)
					{
						num7 = slaveLane2.m_MinIndex;
						num8 = num4;
					}
					else
					{
						num7 = 100000;
						num8 = -100000;
						for (int k = slaveLane2.m_MinIndex; k <= num4; k++)
						{
							Lane lane2 = m_LaneData[dynamicBuffer2[k].m_SubLane];
							if (lane.m_EndNode.Equals(lane2.m_StartNode))
							{
								num7 = math.min(num7, k);
								num8 = k;
							}
						}
					}
					if (num7 <= num8)
					{
						int num9 = num5 + (num7 - slaveLane2.m_MinIndex);
						for (int l = num7; l <= num8; l++)
						{
							num6 = math.min(num6, m_Buffer[num9++]);
						}
						num6 += CalculateLaneObjectCost(laneObjectCost, subLane, currentLaneData.m_CurvePosition.x, currentLaneData.m_LaneFlags);
						if (m_LaneReservationData.HasComponent(subLane))
						{
							num6 += GetLanePriorityCost(m_LaneReservationData[subLane].GetPriority());
						}
					}
					num6 += GetLaneSwitchCost(math.abs(j - num2));
					if (num6 < num3)
					{
						num3 = num6;
						entity = subLane;
					}
				}
			}
			else if ((nextNavLaneData.m_Flags & WatercraftLaneFlags.TransformTarget) != 0 || nextNavLaneData.m_Lane == Entity.Null)
			{
				for (int m = slaveLane.m_MinIndex; m <= num; m++)
				{
					Entity subLane2 = dynamicBuffer[m].m_SubLane;
					float num10 = CalculateLaneObjectCost(laneObjectCost, subLane2, currentLaneData.m_CurvePosition.x, currentLaneData.m_LaneFlags);
					if (m_LaneReservationData.HasComponent(subLane2))
					{
						num10 += GetLanePriorityCost(m_LaneReservationData[subLane2].GetPriority());
					}
					num10 += GetLaneSwitchCost(math.abs(m - num2));
					if (num10 < num3)
					{
						num3 = num10;
						entity = subLane2;
					}
				}
			}
			else
			{
				int num11 = 100000;
				int num12 = -100000;
				if ((nextNavLaneData.m_Flags & WatercraftLaneFlags.GroupTarget) != 0)
				{
					for (int n = slaveLane.m_MinIndex; n <= num; n++)
					{
						if (dynamicBuffer[n].m_SubLane == nextNavLaneData.m_Lane)
						{
							num11 = n;
							num12 = n;
							break;
						}
					}
				}
				else
				{
					Lane lane3 = m_LaneData[nextNavLaneData.m_Lane];
					for (int num13 = slaveLane.m_MinIndex; num13 <= num; num13++)
					{
						if (m_LaneData[dynamicBuffer[num13].m_SubLane].m_EndNode.Equals(lane3.m_StartNode))
						{
							num11 = math.min(num11, num13);
							num12 = num13;
						}
					}
				}
				for (int num14 = slaveLane.m_MinIndex; num14 <= num; num14++)
				{
					Entity subLane3 = dynamicBuffer[num14].m_SubLane;
					float num15;
					if ((num14 >= num11 && num14 <= num12) || num11 > num12)
					{
						num15 = CalculateLaneObjectCost(laneObjectCost, subLane3, currentLaneData.m_CurvePosition.x, currentLaneData.m_LaneFlags);
						if (m_LaneReservationData.HasComponent(subLane3))
						{
							num15 += GetLanePriorityCost(m_LaneReservationData[subLane3].GetPriority());
						}
					}
					else
					{
						num15 = 1000000f;
					}
					num15 += GetLaneSwitchCost(math.abs(num14 - num2));
					if (num15 < num3)
					{
						num3 = num15;
						entity = subLane3;
					}
				}
			}
		}
		if (entity != currentLaneData.m_Lane)
		{
			if (entity != currentLaneData.m_ChangeLane)
			{
				currentLaneData.m_ChangeLane = entity;
				currentLaneData.m_ChangeProgress = 0f;
			}
		}
		else if (currentLaneData.m_ChangeLane != Entity.Null)
		{
			if (currentLaneData.m_ChangeProgress == 0f)
			{
				currentLaneData.m_ChangeLane = Entity.Null;
			}
			else
			{
				currentLaneData.m_Lane = currentLaneData.m_ChangeLane;
				currentLaneData.m_ChangeLane = entity;
				currentLaneData.m_ChangeProgress = math.saturate(1f - currentLaneData.m_ChangeProgress);
			}
		}
		if (currentLaneData.m_ChangeLane == Entity.Null)
		{
			m_PrevLane = currentLaneData.m_Lane;
		}
		else
		{
			m_PrevLane = currentLaneData.m_ChangeLane;
		}
		m_LaneSwitchCost = 10000000f;
	}

	public void UpdateOptimalLane(ref WatercraftNavigationLane navLaneData)
	{
		if (m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
		{
			SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
			if ((navLaneData.m_Flags & (WatercraftLaneFlags.FixedStart | WatercraftLaneFlags.Reserved | WatercraftLaneFlags.FixedLane)) == 0 && m_LaneData.HasComponent(m_PrevLane))
			{
				Owner owner = m_OwnerData[navLaneData.m_Lane];
				DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
				int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
				m_BufferPos -= num - slaveLane.m_MinIndex + 1;
				int num2 = 100000;
				int num3 = -100000;
				if ((navLaneData.m_Flags & WatercraftLaneFlags.GroupTarget) == 0)
				{
					Lane lane = m_LaneData[m_PrevLane];
					for (int i = slaveLane.m_MinIndex; i <= num; i++)
					{
						Lane lane2 = m_LaneData[dynamicBuffer[i].m_SubLane];
						if (lane.m_EndNode.Equals(lane2.m_StartNode))
						{
							num2 = math.min(num2, i);
							num3 = i;
						}
					}
				}
				if (num2 > num3)
				{
					num2 = slaveLane.m_MinIndex;
					num3 = num;
				}
				int bufferPos = m_BufferPos;
				float num4 = float.MaxValue;
				int index = slaveLane.m_MinIndex;
				for (int j = slaveLane.m_MinIndex; j < num2; j++)
				{
					float num5 = m_Buffer[bufferPos++] + GetLaneSwitchCost(num2 - j);
					if (num5 < num4)
					{
						num4 = num5;
						index = j;
					}
				}
				for (int k = num2; k <= num3; k++)
				{
					float num6 = m_Buffer[bufferPos++];
					if (num6 < num4)
					{
						num4 = num6;
						index = k;
					}
				}
				for (int l = num3 + 1; l <= num; l++)
				{
					float num7 = m_Buffer[bufferPos++] + GetLaneSwitchCost(l - num3);
					if (num7 < num4)
					{
						num4 = num7;
						index = l;
					}
				}
				navLaneData.m_Lane = dynamicBuffer[index].m_SubLane;
			}
			m_LaneSwitchCost = m_LaneSwitchBaseCost + math.select(1f, 5f, (slaveLane.m_Flags & SlaveLaneFlags.AllowChange) == 0);
		}
		else
		{
			m_LaneSwitchCost = 10000000f;
		}
		m_PrevLane = navLaneData.m_Lane;
		m_LaneSwitchBaseCost -= 0.01f;
	}

	public void DrawLaneCosts(WatercraftCurrentLane currentLaneData, WatercraftNavigationLane nextNavLaneData, ComponentLookup<Curve> curveData, GizmoBatcher gizmoBatcher)
	{
		if (currentLaneData.m_ChangeProgress == 0f || currentLaneData.m_ChangeLane == Entity.Null)
		{
			m_PrevLane = currentLaneData.m_Lane;
		}
		else
		{
			m_PrevLane = currentLaneData.m_ChangeLane;
		}
	}

	public void DrawLaneCosts(WatercraftNavigationLane navLaneData, ComponentLookup<Curve> curveData, GizmoBatcher gizmoBatcher)
	{
		if (m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
		{
			SlaveLane slaveLane = m_SlaveLaneData[navLaneData.m_Lane];
			Owner owner = m_OwnerData[navLaneData.m_Lane];
			DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[owner.m_Owner];
			int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
			if ((navLaneData.m_Flags & (WatercraftLaneFlags.Reserved | WatercraftLaneFlags.FixedLane)) == 0)
			{
				m_BufferPos -= num - slaveLane.m_MinIndex + 1;
				int bufferPos = m_BufferPos;
				for (int i = slaveLane.m_MinIndex; i <= num; i++)
				{
					float cost = m_Buffer[bufferPos++];
					DrawLane(dynamicBuffer[i].m_SubLane, navLaneData.m_CurvePosition, curveData, gizmoBatcher, cost);
				}
			}
			else
			{
				for (int j = slaveLane.m_MinIndex; j <= num; j++)
				{
					Entity subLane = dynamicBuffer[j].m_SubLane;
					float cost2 = math.select(1000000f, 0f, subLane == navLaneData.m_Lane);
					DrawLane(subLane, navLaneData.m_CurvePosition, curveData, gizmoBatcher, cost2);
				}
			}
		}
		m_PrevLane = navLaneData.m_Lane;
	}

	private void DrawLane(Entity lane, float2 curvePos, ComponentLookup<Curve> curveData, GizmoBatcher gizmoBatcher, float cost)
	{
		Curve curve = curveData[lane];
		UnityEngine.Color color;
		if (cost >= 100000f)
		{
			color = UnityEngine.Color.black;
		}
		else
		{
			cost = math.sqrt(cost);
			color = ((!(cost < 2f)) ? UnityEngine.Color.Lerp(UnityEngine.Color.yellow, UnityEngine.Color.red, (cost - 2f) * 0.5f) : UnityEngine.Color.Lerp(UnityEngine.Color.cyan, UnityEngine.Color.yellow, cost * 0.5f));
		}
		Bezier4x3 bezier = MathUtils.Cut(curve.m_Bezier, curvePos);
		float length = curve.m_Length * math.abs(curvePos.y - curvePos.x);
		gizmoBatcher.DrawCurve(bezier, length, color);
	}
}
