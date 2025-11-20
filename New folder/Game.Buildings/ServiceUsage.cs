using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ServiceUsage : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Usage;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Usage);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Usage);
	}
}
