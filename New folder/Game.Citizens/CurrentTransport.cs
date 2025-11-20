using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct CurrentTransport : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_CurrentTransport;

	public CurrentTransport(Entity transport)
	{
		m_CurrentTransport = transport;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_CurrentTransport);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_CurrentTransport);
	}
}
