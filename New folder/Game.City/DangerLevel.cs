using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct DangerLevel : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_DangerLevel;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_DangerLevel);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_DangerLevel);
	}
}
