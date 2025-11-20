using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct Connected : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Connected;

	public Connected(Entity connected)
	{
		m_Connected = connected;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Connected);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Connected);
	}
}
