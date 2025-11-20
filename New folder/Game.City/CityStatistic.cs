using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct CityStatistic : IBufferElementData, ISerializable
{
	public double m_Value;

	public double m_TotalValue;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		double value = m_Value;
		writer.Write(value);
		double totalValue = m_TotalValue;
		writer.Write(totalValue);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version < Version.statisticOverflowFix)
		{
			reader.Read(out int value);
			reader.Read(out int value2);
			m_Value = value;
			m_TotalValue = value2;
		}
		else if (reader.context.version < Version.statisticPrecisionFix)
		{
			reader.Read(out long value3);
			reader.Read(out long value4);
			m_Value = value3;
			m_TotalValue = value4;
		}
		else
		{
			ref double value5 = ref m_Value;
			reader.Read(out value5);
			ref double totalValue = ref m_TotalValue;
			reader.Read(out totalValue);
		}
	}
}
