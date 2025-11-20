using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Triggers;

public struct RadioEvent : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Entity;

	public RadioEvent(Entity entity)
	{
		m_Entity = entity;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Entity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Entity);
	}
}
