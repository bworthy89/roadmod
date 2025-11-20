using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PostVanData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_MailCapacity;

	public PostVanData(int mailCapacity)
	{
		m_MailCapacity = mailCapacity;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_MailCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_MailCapacity);
	}
}
