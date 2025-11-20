using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Common;

public struct Owner : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Owner;

	public Owner(Entity owner)
	{
		m_Owner = owner;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Owner);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Owner);
	}
}
