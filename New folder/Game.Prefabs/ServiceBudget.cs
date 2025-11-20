using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ServiceBudget : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Budget;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Budget);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Budget);
	}
}
