using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct TelecomFacilityData : IComponentData, IQueryTypeParameter, ICombineData<TelecomFacilityData>, ISerializable
{
	public float m_Range;

	public float m_NetworkCapacity;

	public bool m_PenetrateTerrain;

	public void Combine(TelecomFacilityData otherData)
	{
		m_Range += otherData.m_Range;
		m_NetworkCapacity += otherData.m_NetworkCapacity;
		m_PenetrateTerrain |= otherData.m_PenetrateTerrain;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float range = m_Range;
		writer.Write(range);
		float networkCapacity = m_NetworkCapacity;
		writer.Write(networkCapacity);
		bool penetrateTerrain = m_PenetrateTerrain;
		writer.Write(penetrateTerrain);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float range = ref m_Range;
		reader.Read(out range);
		ref float networkCapacity = ref m_NetworkCapacity;
		reader.Read(out networkCapacity);
		ref bool penetrateTerrain = ref m_PenetrateTerrain;
		reader.Read(out penetrateTerrain);
	}
}
