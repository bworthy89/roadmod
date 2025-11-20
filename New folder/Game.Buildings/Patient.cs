using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Patient : IBufferElementData, IEquatable<Patient>, ISerializable
{
	public Entity m_Patient;

	public Patient(Entity patient)
	{
		m_Patient = patient;
	}

	public bool Equals(Patient other)
	{
		return m_Patient.Equals(other.m_Patient);
	}

	public override int GetHashCode()
	{
		return m_Patient.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Patient);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Patient);
	}
}
