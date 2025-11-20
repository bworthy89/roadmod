using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct AttendingMeeting : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Meeting;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Meeting);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Meeting);
	}
}
