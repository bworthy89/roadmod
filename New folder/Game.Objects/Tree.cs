using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Tree : IComponentData, IQueryTypeParameter, ISerializable
{
	public TreeState m_State;

	public byte m_Growth;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		TreeState state = m_State;
		writer.Write((byte)state);
		byte growth = m_Growth;
		writer.Write(growth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref byte growth = ref m_Growth;
		reader.Read(out growth);
		m_State = (TreeState)value;
	}
}
