using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Watercraft : IComponentData, IQueryTypeParameter, ISerializable
{
	public WatercraftFlags m_Flags;

	public Watercraft(WatercraftFlags flags)
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
		m_Flags = (WatercraftFlags)value;
	}
}
