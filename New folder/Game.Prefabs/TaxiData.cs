using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct TaxiData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_PassengerCapacity;

	public float m_MaintenanceRange;

	public TaxiData(int passengerCapacity, float maintenanceRange)
	{
		m_PassengerCapacity = passengerCapacity;
		m_MaintenanceRange = maintenanceRange;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int passengerCapacity = m_PassengerCapacity;
		writer.Write(passengerCapacity);
		float maintenanceRange = m_MaintenanceRange;
		writer.Write(maintenanceRange);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int passengerCapacity = ref m_PassengerCapacity;
		reader.Read(out passengerCapacity);
		ref float maintenanceRange = ref m_MaintenanceRange;
		reader.Read(out maintenanceRange);
	}
}
