using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

[InternalBufferCapacity(0)]
public struct ServiceDistrict : IBufferElementData, IEquatable<ServiceDistrict>, ISerializable
{
	public Entity m_District;

	public ServiceDistrict(Entity district)
	{
		m_District = district;
	}

	public bool Equals(ServiceDistrict other)
	{
		return m_District.Equals(other.m_District);
	}

	public override int GetHashCode()
	{
		return m_District.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_District);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_District);
	}
}
