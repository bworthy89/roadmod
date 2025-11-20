using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct MailBox : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_CollectRequest;

	public int m_MailAmount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity collectRequest = m_CollectRequest;
		writer.Write(collectRequest);
		int mailAmount = m_MailAmount;
		writer.Write(mailAmount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity collectRequest = ref m_CollectRequest;
		reader.Read(out collectRequest);
		ref int mailAmount = ref m_MailAmount;
		reader.Read(out mailAmount);
	}
}
