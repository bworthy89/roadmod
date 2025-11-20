using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct SewageOutletData : IComponentData, IQueryTypeParameter, ICombineData<SewageOutletData>, ISerializable
{
	public int m_Capacity;

	public float m_Purification;

	public void Combine(SewageOutletData otherData)
	{
		m_Capacity += otherData.m_Capacity;
		m_Purification += otherData.m_Purification;
		m_Purification = math.min(1f, m_Purification);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int capacity = m_Capacity;
		writer.Write(capacity);
		float purification = m_Purification;
		writer.Write(purification);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		ref float purification = ref m_Purification;
		reader.Read(out purification);
	}
}
