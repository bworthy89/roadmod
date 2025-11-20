using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct AggregateElement : IBufferElementData, IEquatable<AggregateElement>, ISerializable
{
	public Entity m_Edge;

	public AggregateElement(Entity edge)
	{
		m_Edge = edge;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Edge);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Edge);
	}

	public bool Equals(AggregateElement other)
	{
		return m_Edge.Equals(other.m_Edge);
	}

	public override int GetHashCode()
	{
		return m_Edge.GetHashCode();
	}
}
