using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Ambulance : IComponentData, IQueryTypeParameter, ISerializable
{
	public AmbulanceFlags m_State;

	public Entity m_TargetPatient;

	public Entity m_TargetLocation;

	public Entity m_TargetRequest;

	public float m_PathElementTime;

	public Ambulance(Entity targetPatient, Entity targetLocation, AmbulanceFlags state)
	{
		m_State = state;
		m_TargetPatient = targetPatient;
		m_TargetLocation = targetLocation;
		m_TargetRequest = Entity.Null;
		m_PathElementTime = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		AmbulanceFlags state = m_State;
		writer.Write((uint)state);
		Entity targetPatient = m_TargetPatient;
		writer.Write(targetPatient);
		Entity targetLocation = m_TargetLocation;
		writer.Write(targetLocation);
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		ref Entity targetPatient = ref m_TargetPatient;
		reader.Read(out targetPatient);
		if (reader.context.version >= Version.healthcareImprovement2)
		{
			ref Entity targetLocation = ref m_TargetLocation;
			reader.Read(out targetLocation);
		}
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		ref float pathElementTime = ref m_PathElementTime;
		reader.Read(out pathElementTime);
		m_State = (AmbulanceFlags)value;
	}
}
