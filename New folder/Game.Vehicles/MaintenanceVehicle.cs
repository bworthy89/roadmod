using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct MaintenanceVehicle : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public MaintenanceVehicleFlags m_State;

	public int m_Maintained;

	public int m_MaintainEstimate;

	public int m_RequestCount;

	public float m_PathElementTime;

	public float m_Efficiency;

	public MaintenanceVehicle(MaintenanceVehicleFlags flags, int requestCount, float efficiency)
	{
		m_TargetRequest = Entity.Null;
		m_State = flags;
		m_Maintained = 0;
		m_MaintainEstimate = 0;
		m_RequestCount = requestCount;
		m_PathElementTime = 0f;
		m_Efficiency = efficiency;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		MaintenanceVehicleFlags state = m_State;
		writer.Write((uint)state);
		int maintained = m_Maintained;
		writer.Write(maintained);
		int maintainEstimate = m_MaintainEstimate;
		writer.Write(maintainEstimate);
		int requestCount = m_RequestCount;
		writer.Write(requestCount);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
		float efficiency = m_Efficiency;
		writer.Write(efficiency);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out uint value);
		ref int maintained = ref m_Maintained;
		reader.Read(out maintained);
		if (reader.context.version >= Version.policeShiftEstimate)
		{
			ref int maintainEstimate = ref m_MaintainEstimate;
			reader.Read(out maintainEstimate);
		}
		ref int requestCount = ref m_RequestCount;
		reader.Read(out requestCount);
		ref float pathElementTime = ref m_PathElementTime;
		reader.Read(out pathElementTime);
		if (reader.context.version >= Version.maintenanceImprovement)
		{
			ref float efficiency = ref m_Efficiency;
			reader.Read(out efficiency);
		}
		m_State = (MaintenanceVehicleFlags)value;
	}
}
