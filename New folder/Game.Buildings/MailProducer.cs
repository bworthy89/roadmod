using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct MailProducer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_MailRequest;

	public ushort m_SendingMail;

	public ushort m_ReceivingMail;

	public byte m_DispatchIndex;

	public ushort m_LastUpdateTotalMail;

	public int receivingMail
	{
		get
		{
			return m_ReceivingMail & 0x7FFF;
		}
		set
		{
			m_ReceivingMail = (ushort)((m_ReceivingMail & 0x8000) | value);
		}
	}

	public bool mailDelivered
	{
		get
		{
			return (m_ReceivingMail & 0x8000) != 0;
		}
		set
		{
			if (value)
			{
				m_ReceivingMail |= 32768;
			}
			else
			{
				m_ReceivingMail &= 32767;
			}
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity mailRequest = m_MailRequest;
		writer.Write(mailRequest);
		ushort sendingMail = m_SendingMail;
		writer.Write(sendingMail);
		ushort value = m_ReceivingMail;
		writer.Write(value);
		byte dispatchIndex = m_DispatchIndex;
		writer.Write(dispatchIndex);
		ushort lastUpdateTotalMail = m_LastUpdateTotalMail;
		writer.Write(lastUpdateTotalMail);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity mailRequest = ref m_MailRequest;
		reader.Read(out mailRequest);
		ref ushort sendingMail = ref m_SendingMail;
		reader.Read(out sendingMail);
		ref ushort value = ref m_ReceivingMail;
		reader.Read(out value);
		if (reader.context.version >= Version.requestDispatchIndex)
		{
			ref byte dispatchIndex = ref m_DispatchIndex;
			reader.Read(out dispatchIndex);
		}
		if (reader.context.format.Has(FormatTags.TrackProcessingMail))
		{
			ref ushort lastUpdateTotalMail = ref m_LastUpdateTotalMail;
			reader.Read(out lastUpdateTotalMail);
		}
		else
		{
			m_LastUpdateTotalMail = 0;
		}
	}
}
