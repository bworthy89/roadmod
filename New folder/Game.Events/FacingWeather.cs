using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct FacingWeather : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public float m_Severity;

	public FacingWeather(Entity _event, float severity)
	{
		m_Event = _event;
		m_Severity = severity;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		float severity = m_Severity;
		writer.Write(severity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref float severity = ref m_Severity;
		reader.Read(out severity);
	}
}
