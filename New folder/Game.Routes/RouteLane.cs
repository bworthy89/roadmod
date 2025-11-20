using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct RouteLane : IComponentData, IQueryTypeParameter, IEquatable<RouteLane>, ISerializable
{
	public Entity m_StartLane;

	public Entity m_EndLane;

	public float m_StartCurvePos;

	public float m_EndCurvePos;

	public RouteLane(Entity startLane, Entity endLane, float startCurvePos, float endCurvePos)
	{
		m_StartLane = startLane;
		m_EndLane = endLane;
		m_StartCurvePos = startCurvePos;
		m_EndCurvePos = endCurvePos;
	}

	public bool Equals(RouteLane other)
	{
		if (m_StartLane.Equals(other.m_StartLane) && m_EndLane.Equals(other.m_EndLane) && m_StartCurvePos.Equals(other.m_StartCurvePos))
		{
			return m_EndCurvePos.Equals(other.m_EndCurvePos);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((17 * 31 + m_StartLane.GetHashCode()) * 31 + m_EndLane.GetHashCode()) * 31 + m_StartCurvePos.GetHashCode()) * 31 + m_EndCurvePos.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity startLane = m_StartLane;
		writer.Write(startLane);
		Entity endLane = m_EndLane;
		writer.Write(endLane);
		float startCurvePos = m_StartCurvePos;
		writer.Write(startCurvePos);
		float endCurvePos = m_EndCurvePos;
		writer.Write(endCurvePos);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity startLane = ref m_StartLane;
		reader.Read(out startLane);
		ref Entity endLane = ref m_EndLane;
		reader.Read(out endLane);
		ref float startCurvePos = ref m_StartCurvePos;
		reader.Read(out startCurvePos);
		ref float endCurvePos = ref m_EndCurvePos;
		reader.Read(out endCurvePos);
	}
}
