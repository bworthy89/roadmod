using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Produced : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Completed;

	public Produced(float completed)
	{
		m_Completed = completed;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Completed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Completed);
	}
}
