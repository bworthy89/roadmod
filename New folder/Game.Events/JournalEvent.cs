using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct JournalEvent : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_JournalEntity;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_JournalEntity);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_JournalEntity);
	}
}
