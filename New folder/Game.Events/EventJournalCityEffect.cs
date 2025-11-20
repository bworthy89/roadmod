using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct EventJournalCityEffect : IBufferElementData, ISerializable
{
	public EventCityEffectTrackingType m_Type;

	public int m_StartValue;

	public int m_Value;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_Type = (EventCityEffectTrackingType)value;
		ref int startValue = ref m_StartValue;
		reader.Read(out startValue);
		ref int value2 = ref m_Value;
		reader.Read(out value2);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		EventCityEffectTrackingType type = m_Type;
		writer.Write((int)type);
		int startValue = m_StartValue;
		writer.Write(startValue);
		int value = m_Value;
		writer.Write(value);
	}
}
