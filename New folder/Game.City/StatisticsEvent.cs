using Colossal.Serialization.Entities;

namespace Game.City;

[FormerlySerializedAs("Game.City.StatisticsEvent2, Game")]
public struct StatisticsEvent : ISerializable
{
	public StatisticType m_Statistic;

	public int m_Parameter;

	public float m_Change;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		StatisticType statistic = m_Statistic;
		writer.Write((int)statistic);
		int parameter = m_Parameter;
		writer.Write(parameter);
		float change = m_Change;
		writer.Write(change);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_Statistic = (StatisticType)value;
		ref int parameter = ref m_Parameter;
		reader.Read(out parameter);
		if (reader.context.version < Version.statisticPrecisionFix)
		{
			reader.Read(out int value2);
			m_Change = value2;
		}
		else
		{
			ref float change = ref m_Change;
			reader.Read(out change);
		}
	}
}
