using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct MailSender : IComponentData, IQueryTypeParameter, ISerializable, IEnableableComponent
{
	public ushort m_Amount;

	public MailSender(ushort amount)
	{
		m_Amount = amount;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Amount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Amount);
	}
}
