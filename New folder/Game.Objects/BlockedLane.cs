using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

[InternalBufferCapacity(0)]
public struct BlockedLane : IBufferElementData, IEquatable<BlockedLane>, ISerializable
{
	public Entity m_Lane;

	public float2 m_CurvePosition;

	public BlockedLane(Entity lane, float2 curvePosition)
	{
		m_Lane = lane;
		m_CurvePosition = curvePosition;
	}

	public bool Equals(BlockedLane other)
	{
		return m_Lane.Equals(other.m_Lane);
	}

	public override int GetHashCode()
	{
		return m_Lane.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float2 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float2 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
	}
}
