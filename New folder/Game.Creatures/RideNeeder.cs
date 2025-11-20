using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct RideNeeder : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_RideRequest;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_RideRequest);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_RideRequest);
	}
}
