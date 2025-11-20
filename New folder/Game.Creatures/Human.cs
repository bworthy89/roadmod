using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct Human : IComponentData, IQueryTypeParameter, ISerializable
{
	public HumanFlags m_Flags;

	public Human(HumanFlags flags)
	{
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((uint)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_Flags = (HumanFlags)value;
	}
}
