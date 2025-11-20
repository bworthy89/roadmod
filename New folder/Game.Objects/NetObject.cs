using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct NetObject : IComponentData, IQueryTypeParameter, ISerializable
{
	public NetObjectFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_Flags = (NetObjectFlags)value;
	}
}
