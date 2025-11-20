using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct CoordinatedMeeting : IComponentData, IQueryTypeParameter, ISerializable
{
	public MeetingStatus m_Status;

	public int m_Phase;

	public Entity m_Target;

	public uint m_PhaseEndTime;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_Status = (MeetingStatus)value;
		if (reader.context.version < Version.timoSerializationFlow)
		{
			reader.Read(out TravelPurpose _);
		}
		ref Entity target = ref m_Target;
		reader.Read(out target);
		if (reader.context.version < Version.timoSerializationFlow)
		{
			m_Phase = 0;
			m_PhaseEndTime = 0u;
			return;
		}
		ref int phase = ref m_Phase;
		reader.Read(out phase);
		ref uint phaseEndTime = ref m_PhaseEndTime;
		reader.Read(out phaseEndTime);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		MeetingStatus status = m_Status;
		writer.Write((int)status);
		Entity target = m_Target;
		writer.Write(target);
		int phase = m_Phase;
		writer.Write(phase);
		uint phaseEndTime = m_PhaseEndTime;
		writer.Write(phaseEndTime);
	}
}
