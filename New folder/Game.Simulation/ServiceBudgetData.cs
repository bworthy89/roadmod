using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ServiceBudgetData : IBufferElementData, ISerializable
{
	public Entity m_Service;

	public int m_Budget;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity service = ref m_Service;
		reader.Read(out service);
		ref int budget = ref m_Budget;
		reader.Read(out budget);
		if (reader.context.version < Version.serviceImportBudgets)
		{
			reader.Read(out int _);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity service = m_Service;
		writer.Write(service);
		int budget = m_Budget;
		writer.Write(budget);
	}
}
