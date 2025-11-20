using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct GarbageFacilityData : IComponentData, IQueryTypeParameter, ICombineData<GarbageFacilityData>, ISerializable
{
	public int m_GarbageCapacity;

	public int m_VehicleCapacity;

	public int m_TransportCapacity;

	public int m_ProcessingSpeed;

	public bool m_IndustrialWasteOnly;

	public bool m_LongTermStorage;

	public void Combine(GarbageFacilityData otherData)
	{
		m_GarbageCapacity += otherData.m_GarbageCapacity;
		m_VehicleCapacity += otherData.m_VehicleCapacity;
		m_TransportCapacity += otherData.m_TransportCapacity;
		m_ProcessingSpeed += otherData.m_ProcessingSpeed;
		m_IndustrialWasteOnly |= otherData.m_IndustrialWasteOnly;
		m_LongTermStorage |= otherData.m_LongTermStorage;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int garbageCapacity = m_GarbageCapacity;
		writer.Write(garbageCapacity);
		int vehicleCapacity = m_VehicleCapacity;
		writer.Write(vehicleCapacity);
		int transportCapacity = m_TransportCapacity;
		writer.Write(transportCapacity);
		int processingSpeed = m_ProcessingSpeed;
		writer.Write(processingSpeed);
		bool industrialWasteOnly = m_IndustrialWasteOnly;
		writer.Write(industrialWasteOnly);
		bool longTermStorage = m_LongTermStorage;
		writer.Write(longTermStorage);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int garbageCapacity = ref m_GarbageCapacity;
		reader.Read(out garbageCapacity);
		ref int vehicleCapacity = ref m_VehicleCapacity;
		reader.Read(out vehicleCapacity);
		ref int transportCapacity = ref m_TransportCapacity;
		reader.Read(out transportCapacity);
		ref int processingSpeed = ref m_ProcessingSpeed;
		reader.Read(out processingSpeed);
		ref bool industrialWasteOnly = ref m_IndustrialWasteOnly;
		reader.Read(out industrialWasteOnly);
		ref bool longTermStorage = ref m_LongTermStorage;
		reader.Read(out longTermStorage);
	}
}
