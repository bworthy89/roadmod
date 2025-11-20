using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Triggers;

[InternalBufferCapacity(2)]
public struct ChirpEntity : IBufferElementData, ISerializable
{
	public Entity m_Entity;

	public ChirpEntity(Entity entity)
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
