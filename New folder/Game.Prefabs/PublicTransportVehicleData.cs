using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PublicTransportVehicleData : IComponentData, IQueryTypeParameter, ISerializable
{
	public TransportType m_TransportType;

	public int m_PassengerCapacity;

	public PublicTransportPurpose m_PurposeMask;

	public float m_MaintenanceRange;

	public PublicTransportVehicleData(TransportType type, int passengerCapacity, PublicTransportPurpose purposeMask, float maintenanceRange)
	{
		m_TransportType = type;
		m_PassengerCapacity = passengerCapacity;
		m_PurposeMask = purposeMask;
		m_MaintenanceRange = maintenanceRange;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)m_TransportType;
		writer.Write(value);
		PublicTransportPurpose purposeMask = m_PurposeMask;
		writer.Write((uint)purposeMask);
		int passengerCapacity = m_PassengerCapacity;
		writer.Write(passengerCapacity);
		float maintenanceRange = m_MaintenanceRange;
		writer.Write(maintenanceRange);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		reader.Read(out uint value2);
		ref int passengerCapacity = ref m_PassengerCapacity;
		reader.Read(out passengerCapacity);
		ref float maintenanceRange = ref m_MaintenanceRange;
		reader.Read(out maintenanceRange);
		m_TransportType = (TransportType)value;
		m_PurposeMask = (PublicTransportPurpose)value2;
	}
}
