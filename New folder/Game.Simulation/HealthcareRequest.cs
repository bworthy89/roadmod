using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct HealthcareRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Citizen;

	public HealthcareRequestType m_Type;

	public HealthcareRequest(Entity citizen, HealthcareRequestType type)
	{
		m_Citizen = citizen;
		m_Type = type;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity citizen = m_Citizen;
		writer.Write(citizen);
		HealthcareRequestType type = m_Type;
		writer.Write((byte)type);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity citizen = ref m_Citizen;
		reader.Read(out citizen);
		reader.Read(out byte value);
		m_Type = (HealthcareRequestType)value;
	}
}
