using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ResourceConsumer : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_ResourceAvailability;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_ResourceAvailability);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_ResourceAvailability);
	}
}
