using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct CollectedCityServiceBudgetData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int3 m_Workplaces;

	public int m_Count;

	public int m_Export;

	public int m_BaseCost;

	public int m_Wages;

	public int m_FullWages;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int3 workplaces = ref m_Workplaces;
		reader.Read(out workplaces);
		ref int count = ref m_Count;
		reader.Read(out count);
		ref int wages = ref m_Wages;
		reader.Read(out wages);
		ref int fullWages = ref m_FullWages;
		reader.Read(out fullWages);
		ref int export = ref m_Export;
		reader.Read(out export);
		if (reader.context.version >= Version.netUpkeepCost)
		{
			ref int baseCost = ref m_BaseCost;
			reader.Read(out baseCost);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int3 workplaces = m_Workplaces;
		writer.Write(workplaces);
		int count = m_Count;
		writer.Write(count);
		int wages = m_Wages;
		writer.Write(wages);
		int fullWages = m_FullWages;
		writer.Write(fullWages);
		int export = m_Export;
		writer.Write(export);
		int baseCost = m_BaseCost;
		writer.Write(baseCost);
	}
}
