using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct EmergencyShelterData : IComponentData, IQueryTypeParameter, ICombineData<EmergencyShelterData>, ISerializable
{
	public int m_ShelterCapacity;

	public int m_VehicleCapacity;

	public void Combine(EmergencyShelterData otherData)
	{
		m_ShelterCapacity += otherData.m_ShelterCapacity;
		m_VehicleCapacity += otherData.m_VehicleCapacity;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int shelterCapacity = m_ShelterCapacity;
		writer.Write(shelterCapacity);
		int vehicleCapacity = m_VehicleCapacity;
		writer.Write(vehicleCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int shelterCapacity = ref m_ShelterCapacity;
		reader.Read(out shelterCapacity);
		ref int vehicleCapacity = ref m_VehicleCapacity;
		reader.Read(out vehicleCapacity);
	}
}
