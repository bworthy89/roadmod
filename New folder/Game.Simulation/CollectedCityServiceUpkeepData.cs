using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Simulation;

public struct CollectedCityServiceUpkeepData : IBufferElementData, ISerializable
{
	public Resource m_Resource;

	public int m_FullCost;

	public int m_Amount;

	public int m_Cost;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_Resource = (Resource)value;
		ref int fullCost = ref m_FullCost;
		reader.Read(out fullCost);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		ref int cost = ref m_Cost;
		reader.Read(out cost);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = (int)m_Resource;
		writer.Write(value);
		int fullCost = m_FullCost;
		writer.Write(fullCost);
		int amount = m_Amount;
		writer.Write(amount);
		int cost = m_Cost;
		writer.Write(cost);
	}
}
