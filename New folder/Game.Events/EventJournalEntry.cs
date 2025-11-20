using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct EventJournalEntry : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public uint m_StartFrame;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref uint startFrame = ref m_StartFrame;
		reader.Read(out startFrame);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		uint startFrame = m_StartFrame;
		writer.Write(startFrame);
	}
}
