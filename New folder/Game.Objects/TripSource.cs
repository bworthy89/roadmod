using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct TripSource : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Source;

	public int m_Timer;

	public TripSource(Entity source)
	{
		m_Source = source;
		m_Timer = 0;
	}

	public TripSource(Entity source, uint delay)
	{
		m_Source = source;
		m_Timer = (int)delay;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity source = m_Source;
		writer.Write(source);
		int timer = m_Timer;
		writer.Write(timer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity source = ref m_Source;
		reader.Read(out source);
		ref int timer = ref m_Timer;
		reader.Read(out timer);
	}
}
