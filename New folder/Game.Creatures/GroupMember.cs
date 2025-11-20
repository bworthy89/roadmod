using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct GroupMember : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Leader;

	public GroupMember(Entity leader)
	{
		m_Leader = leader;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Leader);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Leader);
	}
}
