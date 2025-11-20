using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct AttendingEvent : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Event);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Event);
	}
}
