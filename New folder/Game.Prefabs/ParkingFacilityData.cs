using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct ParkingFacilityData : IComponentData, IQueryTypeParameter, ICombineData<ParkingFacilityData>, ISerializable
{
	public RoadTypes m_RoadTypes;

	public float m_ComfortFactor;

	public int m_GarageMarkerCapacity;

	public void Combine(ParkingFacilityData otherData)
	{
		m_RoadTypes |= otherData.m_RoadTypes;
		m_ComfortFactor += otherData.m_ComfortFactor;
		m_GarageMarkerCapacity += otherData.m_GarageMarkerCapacity;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float comfortFactor = m_ComfortFactor;
		writer.Write(comfortFactor);
		int garageMarkerCapacity = m_GarageMarkerCapacity;
		writer.Write(garageMarkerCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float comfortFactor = ref m_ComfortFactor;
		reader.Read(out comfortFactor);
		ref int garageMarkerCapacity = ref m_GarageMarkerCapacity;
		reader.Read(out garageMarkerCapacity);
	}
}
