using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct CargoTransportVehicleData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Resource m_Resources;

	public int m_CargoCapacity;

	public int m_MaxResourceCount;

	public float m_MaintenanceRange;

	public CargoTransportVehicleData(Resource resources, int cargoCapacity, int maxResourceCount, float maintenanceRange)
	{
		m_Resources = resources;
		m_CargoCapacity = cargoCapacity;
		m_MaxResourceCount = maxResourceCount;
		m_MaintenanceRange = maintenanceRange;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Resource resources = m_Resources;
		writer.Write((ulong)resources);
		int cargoCapacity = m_CargoCapacity;
		writer.Write(cargoCapacity);
		int maxResourceCount = m_MaxResourceCount;
		writer.Write(maxResourceCount);
		float maintenanceRange = m_MaintenanceRange;
		writer.Write(maintenanceRange);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out ulong value);
		ref int cargoCapacity = ref m_CargoCapacity;
		reader.Read(out cargoCapacity);
		ref int maxResourceCount = ref m_MaxResourceCount;
		reader.Read(out maxResourceCount);
		ref float maintenanceRange = ref m_MaintenanceRange;
		reader.Read(out maintenanceRange);
		m_Resources = (Resource)value;
	}
}
