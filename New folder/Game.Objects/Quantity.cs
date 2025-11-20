using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Quantity : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_Fullness;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Fullness);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Fullness);
	}
}
