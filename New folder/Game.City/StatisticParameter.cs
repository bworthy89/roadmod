using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct StatisticParameter : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Value;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Value);
	}
}
