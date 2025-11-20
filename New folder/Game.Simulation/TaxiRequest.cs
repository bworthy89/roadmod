using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct TaxiRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Seeker;

	public Entity m_District1;

	public Entity m_District2;

	public int m_Priority;

	public TaxiRequestType m_Type;

	public TaxiRequest(Entity seeker, Entity district1, Entity district2, TaxiRequestType type, int priority)
	{
		m_Seeker = seeker;
		m_District1 = district1;
		m_District2 = district2;
		m_Priority = priority;
		m_Type = type;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity seeker = m_Seeker;
		writer.Write(seeker);
		Entity district = m_District1;
		writer.Write(district);
		Entity district2 = m_District2;
		writer.Write(district2);
		int priority = m_Priority;
		writer.Write(priority);
		TaxiRequestType type = m_Type;
		writer.Write((byte)type);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity seeker = ref m_Seeker;
		reader.Read(out seeker);
		if (reader.context.version >= Version.taxiServiceDistricts)
		{
			ref Entity district = ref m_District1;
			reader.Read(out district);
			ref Entity district2 = ref m_District2;
			reader.Read(out district2);
		}
		ref int priority = ref m_Priority;
		reader.Read(out priority);
		if (reader.context.version >= Version.taxiDispatchCenter)
		{
			reader.Read(out byte value);
			m_Type = (TaxiRequestType)value;
		}
	}
}
