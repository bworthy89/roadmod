using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Notifications;

public struct Animation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Timer;

	public float m_Duration;

	public AnimationType m_Type;

	public Animation(AnimationType type, float timer, float duration)
	{
		m_Timer = timer;
		m_Duration = duration;
		m_Type = type;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float timer = m_Timer;
		writer.Write(timer);
		float duration = m_Duration;
		writer.Write(duration);
		AnimationType type = m_Type;
		writer.Write((byte)type);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float timer = ref m_Timer;
		reader.Read(out timer);
		ref float duration = ref m_Duration;
		reader.Read(out duration);
		reader.Read(out byte value);
		m_Type = (AnimationType)value;
	}
}
