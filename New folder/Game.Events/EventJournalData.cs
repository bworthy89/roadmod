using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct EventJournalData : IBufferElementData, ISerializable
{
	public EventDataTrackingType m_Type;

	public int m_Value;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_Type = (EventDataTrackingType)value;
		ref int value2 = ref m_Value;
		reader.Read(out value2);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		EventDataTrackingType type = m_Type;
		writer.Write((int)type);
		int value = m_Value;
		writer.Write(value);
	}
}
