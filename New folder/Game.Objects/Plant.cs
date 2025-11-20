using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Plant : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Pollution;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Pollution);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Pollution);
	}
}
