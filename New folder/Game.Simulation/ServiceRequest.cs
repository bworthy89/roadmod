using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ServiceRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_FailCount;

	public byte m_Cooldown;

	public ServiceRequestFlags m_Flags;

	public ServiceRequest(bool reversed)
	{
		m_FailCount = 0;
		m_Cooldown = 0;
		m_Flags = (reversed ? ServiceRequestFlags.Reversed : ((ServiceRequestFlags)0));
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte failCount = m_FailCount;
		writer.Write(failCount);
		byte cooldown = m_Cooldown;
		writer.Write(cooldown);
		ServiceRequestFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref byte failCount = ref m_FailCount;
		reader.Read(out failCount);
		ref byte cooldown = ref m_Cooldown;
		reader.Read(out cooldown);
		if (reader.context.version >= Version.reverseServiceRequests)
		{
			reader.Read(out byte value);
			m_Flags = (ServiceRequestFlags)value;
		}
	}
}
