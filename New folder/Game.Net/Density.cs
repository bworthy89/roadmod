using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct Density : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Density;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Density);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Density);
	}
}
