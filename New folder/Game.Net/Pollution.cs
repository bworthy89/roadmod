using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct Pollution : IComponentData, IQueryTypeParameter, ISerializable
{
	public float2 m_Pollution;

	public float2 m_Accumulation;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float2 pollution = m_Pollution;
		writer.Write(pollution);
		float2 accumulation = m_Accumulation;
		writer.Write(accumulation);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float2 pollution = ref m_Pollution;
		reader.Read(out pollution);
		if (reader.context.version >= Version.netPollutionAccumulation)
		{
			ref float2 accumulation = ref m_Accumulation;
			reader.Read(out accumulation);
		}
		else
		{
			m_Accumulation = m_Pollution * 2f;
		}
	}
}
