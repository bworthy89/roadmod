using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct WaterPoweredData : IComponentData, IQueryTypeParameter, ICombineData<WaterPoweredData>, ISerializable
{
	public float m_ProductionFactor;

	public float m_CapacityFactor;

	public void Combine(WaterPoweredData otherData)
	{
		m_ProductionFactor += otherData.m_ProductionFactor;
		m_CapacityFactor += otherData.m_CapacityFactor;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float productionFactor = m_ProductionFactor;
		writer.Write(productionFactor);
		float capacityFactor = m_CapacityFactor;
		writer.Write(capacityFactor);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float productionFactor = ref m_ProductionFactor;
		reader.Read(out productionFactor);
		ref float capacityFactor = ref m_CapacityFactor;
		reader.Read(out capacityFactor);
	}
}
