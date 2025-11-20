using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Buildings;

public struct PoliceStation : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_PrisonerTransportRequest;

	public Entity m_TargetRequest;

	public PoliceStationFlags m_Flags;

	public PolicePurpose m_PurposeMask;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity prisonerTransportRequest = m_PrisonerTransportRequest;
		writer.Write(prisonerTransportRequest);
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		PoliceStationFlags flags = m_Flags;
		writer.Write((byte)flags);
		PolicePurpose purposeMask = m_PurposeMask;
		writer.Write((int)purposeMask);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.policeImprovement2)
		{
			ref Entity prisonerTransportRequest = ref m_PrisonerTransportRequest;
			reader.Read(out prisonerTransportRequest);
		}
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value);
		m_Flags = (PoliceStationFlags)value;
		if (reader.context.version >= Version.policeImprovement3)
		{
			reader.Read(out int value2);
			m_PurposeMask = (PolicePurpose)value2;
		}
		else
		{
			m_PurposeMask = PolicePurpose.Patrol | PolicePurpose.Emergency;
		}
	}
}
