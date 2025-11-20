using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PowerPlantData : IComponentData, IQueryTypeParameter, ICombineData<PowerPlantData>, ISerializable
{
	public int m_ElectricityProduction;

	public void Combine(PowerPlantData otherData)
	{
		m_ElectricityProduction += otherData.m_ElectricityProduction;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_ElectricityProduction);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_ElectricityProduction);
	}
}
