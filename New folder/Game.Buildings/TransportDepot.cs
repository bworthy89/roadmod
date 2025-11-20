using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct TransportDepot : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public TransportDepotFlags m_Flags;

	public byte m_AvailableVehicles;

	public float m_MaintenanceRequirement;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		TransportDepotFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte availableVehicles = m_AvailableVehicles;
		writer.Write(availableVehicles);
		float maintenanceRequirement = m_MaintenanceRequirement;
		writer.Write(maintenanceRequirement);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value);
		if (reader.context.version >= Version.taxiDispatchCenter)
		{
			ref byte availableVehicles = ref m_AvailableVehicles;
			reader.Read(out availableVehicles);
		}
		if (reader.context.version >= Version.transportMaintenance)
		{
			ref float maintenanceRequirement = ref m_MaintenanceRequirement;
			reader.Read(out maintenanceRequirement);
		}
		m_Flags = (TransportDepotFlags)value;
	}
}
