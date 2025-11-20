using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct MilestoneLevel : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_AchievedMilestone;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_AchievedMilestone);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_AchievedMilestone);
	}
}
