using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct WastewaterTreatmentPlantData : IComponentData, IQueryTypeParameter, ICombineData<WastewaterTreatmentPlantData>, ISerializable
{
	public int m_Capacity;

	public int m_WaterStorage;

	public void Combine(WastewaterTreatmentPlantData otherData)
	{
		m_Capacity += otherData.m_Capacity;
		m_WaterStorage += otherData.m_WaterStorage;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int capacity = m_Capacity;
		writer.Write(capacity);
		int waterStorage = m_WaterStorage;
		writer.Write(waterStorage);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		ref int waterStorage = ref m_WaterStorage;
		reader.Read(out waterStorage);
	}
}
