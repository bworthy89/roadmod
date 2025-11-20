using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct SpectatorSite : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public SpectatorSite(Entity _event)
	{
		m_Event = _event;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Event);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Event);
	}
}
