using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

[InternalBufferCapacity(0)]
public struct LayoutElement : IBufferElementData, IEquatable<LayoutElement>, ISerializable
{
	public Entity m_Vehicle;

	public LayoutElement(Entity vehicle)
	{
		m_Vehicle = vehicle;
	}

	public bool Equals(LayoutElement other)
	{
		return m_Vehicle.Equals(other.m_Vehicle);
	}

	public override int GetHashCode()
	{
		return m_Vehicle.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Vehicle);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Vehicle);
	}
}
