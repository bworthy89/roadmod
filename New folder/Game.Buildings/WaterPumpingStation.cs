using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct WaterPumpingStation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Pollution;

	public int m_Capacity;

	public int m_LastProduction;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float pollution = m_Pollution;
		writer.Write(pollution);
		int capacity = m_Capacity;
		writer.Write(capacity);
		int lastProduction = m_LastProduction;
		writer.Write(lastProduction);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version < Version.waterPipeFlowSim)
		{
			reader.Read(out int _);
		}
		if (reader.context.version >= Version.waterPipePollution)
		{
			ref float pollution = ref m_Pollution;
			reader.Read(out pollution);
		}
		else
		{
			reader.Read(out int _);
		}
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		if (reader.context.version >= Version.waterSelectedInfoFix)
		{
			if (reader.context.version < Version.waterPipeFlowSim)
			{
				reader.Read(out int _);
			}
			ref int lastProduction = ref m_LastProduction;
			reader.Read(out lastProduction);
		}
	}
}
