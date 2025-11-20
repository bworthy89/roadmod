using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Achievements;

public struct EventAchievementTrackingData : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_StartFrame;

	public AchievementId m_ID;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint startFrame = ref m_StartFrame;
		reader.Read(out startFrame);
		reader.Read(out int value);
		m_ID = new AchievementId(value);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint startFrame = m_StartFrame;
		writer.Write(startFrame);
		int id = m_ID.id;
		writer.Write(id);
	}
}
