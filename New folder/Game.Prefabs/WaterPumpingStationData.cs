using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct WaterPumpingStationData : IComponentData, IQueryTypeParameter, ICombineData<WaterPumpingStationData>, ISerializable
{
	public AllowedWaterTypes m_Types;

	public int m_Capacity;

	public float m_Purification;

	public void Combine(WaterPumpingStationData otherData)
	{
		m_Types |= otherData.m_Types;
		m_Capacity += otherData.m_Capacity;
		m_Purification = 1f - (1f - m_Purification) * (1f - otherData.m_Purification);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int capacity = m_Capacity;
		writer.Write(capacity);
		float purification = m_Purification;
		writer.Write(purification);
		ushort value = (ushort)m_Types;
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		ref float purification = ref m_Purification;
		reader.Read(out purification);
		reader.Read(out ushort value);
		m_Types = (AllowedWaterTypes)value;
	}
}
