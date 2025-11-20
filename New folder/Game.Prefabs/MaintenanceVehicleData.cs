using Colossal.Serialization.Entities;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

public struct MaintenanceVehicleData : IComponentData, IQueryTypeParameter, ISerializable
{
	public MaintenanceType m_MaintenanceType;

	public int m_MaintenanceCapacity;

	public int m_MaintenanceRate;

	public MaintenanceVehicleData(MaintenanceType maintenanceType, int maintenanceCapacity, int maintenanceRate)
	{
		m_MaintenanceType = maintenanceType;
		m_MaintenanceCapacity = maintenanceCapacity;
		m_MaintenanceRate = maintenanceRate;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		MaintenanceType maintenanceType = m_MaintenanceType;
		writer.Write((byte)maintenanceType);
		int maintenanceCapacity = m_MaintenanceCapacity;
		writer.Write(maintenanceCapacity);
		int maintenanceRate = m_MaintenanceRate;
		writer.Write(maintenanceRate);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref int maintenanceCapacity = ref m_MaintenanceCapacity;
		reader.Read(out maintenanceCapacity);
		ref int maintenanceRate = ref m_MaintenanceRate;
		reader.Read(out maintenanceRate);
		m_MaintenanceType = (MaintenanceType)value;
	}
}
