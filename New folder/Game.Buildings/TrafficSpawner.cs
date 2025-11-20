using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct TrafficSpawner : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TrafficRequest;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_TrafficRequest);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_TrafficRequest);
	}
}
