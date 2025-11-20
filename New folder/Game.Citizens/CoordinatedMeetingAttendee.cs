using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct CoordinatedMeetingAttendee : IBufferElementData, ISerializable
{
	public Entity m_Attendee;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Attendee);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Attendee);
	}
}
