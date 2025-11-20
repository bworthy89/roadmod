using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Occupant : IBufferElementData, IEquatable<Occupant>, ISerializable
{
	public Entity m_Occupant;

	public Occupant(Entity occupant)
	{
		m_Occupant = occupant;
	}

	public bool Equals(Occupant other)
	{
		return m_Occupant.Equals(other.m_Occupant);
	}

	public override int GetHashCode()
	{
		return m_Occupant.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Occupant);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Occupant);
	}

	public static implicit operator Entity(Occupant occupant)
	{
		return occupant.m_Occupant;
	}
}
