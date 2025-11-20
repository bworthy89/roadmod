using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct EventJournalPending : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_StartFrame;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_StartFrame);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_StartFrame);
	}
}
