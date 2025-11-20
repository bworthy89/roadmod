using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct ConnectedNode : IBufferElementData, IEquatable<ConnectedNode>, ISerializable
{
	public Entity m_Node;

	public float m_CurvePosition;

	public ConnectedNode(Entity node, float curvePosition)
	{
		m_Node = node;
		m_CurvePosition = curvePosition;
	}

	public bool Equals(ConnectedNode other)
	{
		return m_Node.Equals(other.m_Node);
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_Node.GetHashCode()) * 31 + m_CurvePosition.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity node = m_Node;
		writer.Write(node);
		float curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity node = ref m_Node;
		reader.Read(out node);
		ref float curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
	}
}
