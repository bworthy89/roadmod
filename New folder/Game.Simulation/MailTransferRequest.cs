using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct MailTransferRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Facility;

	public MailTransferRequestFlags m_Flags;

	public float m_Priority;

	public int m_Amount;

	public MailTransferRequest(Entity facility, MailTransferRequestFlags flags, float priority, int amount)
	{
		m_Facility = facility;
		m_Flags = flags;
		m_Priority = priority;
		m_Amount = amount;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity facility = m_Facility;
		writer.Write(facility);
		MailTransferRequestFlags flags = m_Flags;
		writer.Write((ushort)flags);
		float priority = m_Priority;
		writer.Write(priority);
		int amount = m_Amount;
		writer.Write(amount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity facility = ref m_Facility;
		reader.Read(out facility);
		reader.Read(out ushort value);
		ref float priority = ref m_Priority;
		reader.Read(out priority);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		m_Flags = (MailTransferRequestFlags)value;
	}
}
