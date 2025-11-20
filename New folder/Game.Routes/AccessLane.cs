using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct AccessLane : IComponentData, IQueryTypeParameter, IEquatable<AccessLane>, ISerializable
{
	public Entity m_Lane;

	public float m_CurvePos;

	public AccessLane(Entity lane, float curvePos)
	{
		m_Lane = lane;
		m_CurvePos = curvePos;
	}

	public bool Equals(AccessLane other)
	{
		if (m_Lane.Equals(other.m_Lane))
		{
			return m_CurvePos.Equals(other.m_CurvePos);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_Lane.GetHashCode()) * 31 + m_CurvePos.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float curvePos = m_CurvePos;
		writer.Write(curvePos);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float curvePos = ref m_CurvePos;
		reader.Read(out curvePos);
	}
}
