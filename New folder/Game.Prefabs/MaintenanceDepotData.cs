using Colossal.Serialization.Entities;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

public struct MaintenanceDepotData : IComponentData, IQueryTypeParameter, ICombineData<MaintenanceDepotData>, ISerializable
{
	public MaintenanceType m_MaintenanceType;

	public int m_VehicleCapacity;

	public float m_VehicleEfficiency;

	public void Combine(MaintenanceDepotData otherData)
	{
		m_MaintenanceType |= otherData.m_MaintenanceType;
		m_VehicleCapacity += otherData.m_VehicleCapacity;
		m_VehicleEfficiency += otherData.m_VehicleEfficiency;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int vehicleCapacity = m_VehicleCapacity;
		writer.Write(vehicleCapacity);
		float vehicleEfficiency = m_VehicleEfficiency;
		writer.Write(vehicleEfficiency);
		MaintenanceType maintenanceType = m_MaintenanceType;
		writer.Write((byte)maintenanceType);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int vehicleCapacity = ref m_VehicleCapacity;
		reader.Read(out vehicleCapacity);
		ref float vehicleEfficiency = ref m_VehicleEfficiency;
		reader.Read(out vehicleEfficiency);
		reader.Read(out byte value);
		m_MaintenanceType = (MaintenanceType)value;
	}
}
