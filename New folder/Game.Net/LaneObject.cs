using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct LaneObject : IBufferElementData, IEquatable<LaneObject>, IComparable<LaneObject>, IEmptySerializable
{
	public Entity m_LaneObject;

	public float2 m_CurvePosition;

	public LaneObject(Entity laneObject)
	{
		m_LaneObject = laneObject;
		m_CurvePosition = default(float2);
	}

	public LaneObject(Entity laneObject, float2 curvePosition)
	{
		m_LaneObject = laneObject;
		m_CurvePosition = curvePosition;
	}

	public bool Equals(LaneObject other)
	{
		return m_LaneObject.Equals(other.m_LaneObject);
	}

	public int CompareTo(LaneObject other)
	{
		return (int)math.sign(m_CurvePosition.x - other.m_CurvePosition.x);
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_LaneObject.GetHashCode()) * 31 + m_CurvePosition.GetHashCode();
	}
}
