using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct LaneObjectAction : IComparable<LaneObjectAction>
{
	public Entity m_Lane;

	public Entity m_Remove;

	public Entity m_Add;

	public float2 m_CurvePosition;

	public LaneObjectAction(Entity lane, Entity remove)
	{
		m_Lane = lane;
		m_Remove = remove;
		m_Add = Entity.Null;
		m_CurvePosition = default(float2);
	}

	public LaneObjectAction(Entity lane, Entity add, float2 curvePosition)
	{
		m_Lane = lane;
		m_Remove = Entity.Null;
		m_Add = add;
		m_CurvePosition = curvePosition;
	}

	public LaneObjectAction(Entity lane, Entity remove, Entity add, float2 curvePosition)
	{
		m_Lane = lane;
		m_Remove = remove;
		m_Add = add;
		m_CurvePosition = curvePosition;
	}

	public int CompareTo(LaneObjectAction other)
	{
		return m_Lane.Index - other.m_Lane.Index;
	}

	public override int GetHashCode()
	{
		return m_Lane.GetHashCode();
	}
}
