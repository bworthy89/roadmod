using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct CollectedServiceBuildingBudgetData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Count;

	public int m_Workers;

	public int m_Workplaces;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int count = ref m_Count;
		reader.Read(out count);
		ref int workers = ref m_Workers;
		reader.Read(out workers);
		ref int workplaces = ref m_Workplaces;
		reader.Read(out workplaces);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int count = m_Count;
		writer.Write(count);
		int workers = m_Workers;
		writer.Write(workers);
		int workplaces = m_Workplaces;
		writer.Write(workplaces);
	}
}
