using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct PostFacility : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_MailDeliverRequest;

	public Entity m_MailReceiveRequest;

	public Entity m_TargetRequest;

	public float m_AcceptMailPriority;

	public float m_DeliverMailPriority;

	public PostFacilityFlags m_Flags;

	public byte m_ProcessingFactor;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity mailDeliverRequest = m_MailDeliverRequest;
		writer.Write(mailDeliverRequest);
		Entity mailReceiveRequest = m_MailReceiveRequest;
		writer.Write(mailReceiveRequest);
		float acceptMailPriority = m_AcceptMailPriority;
		writer.Write(acceptMailPriority);
		float deliverMailPriority = m_DeliverMailPriority;
		writer.Write(deliverMailPriority);
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		PostFacilityFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte processingFactor = m_ProcessingFactor;
		writer.Write(processingFactor);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.transferRequestRefactoring)
		{
			ref Entity mailDeliverRequest = ref m_MailDeliverRequest;
			reader.Read(out mailDeliverRequest);
			ref Entity mailReceiveRequest = ref m_MailReceiveRequest;
			reader.Read(out mailReceiveRequest);
			ref float acceptMailPriority = ref m_AcceptMailPriority;
			reader.Read(out acceptMailPriority);
			ref float deliverMailPriority = ref m_DeliverMailPriority;
			reader.Read(out deliverMailPriority);
		}
		else
		{
			reader.Read(out Entity _);
		}
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value2);
		if (reader.context.version >= Version.mailProcessing)
		{
			ref byte processingFactor = ref m_ProcessingFactor;
			reader.Read(out processingFactor);
		}
		m_Flags = (PostFacilityFlags)value2;
	}
}
