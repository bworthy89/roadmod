using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Aligned : IComponentData, IQueryTypeParameter, ISerializable
{
	public ushort m_SubObjectIndex;

	public Aligned(ushort subObjectIndex)
	{
		m_SubObjectIndex = subObjectIndex;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_SubObjectIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_SubObjectIndex);
	}
}
