using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct Road : IComponentData, IQueryTypeParameter, ISerializable
{
	public float4 m_TrafficFlowDuration0;

	public float4 m_TrafficFlowDuration1;

	public float4 m_TrafficFlowDistance0;

	public float4 m_TrafficFlowDistance1;

	public RoadFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float4 trafficFlowDuration = m_TrafficFlowDuration0;
		writer.Write(trafficFlowDuration);
		float4 trafficFlowDuration2 = m_TrafficFlowDuration1;
		writer.Write(trafficFlowDuration2);
		float4 trafficFlowDistance = m_TrafficFlowDistance0;
		writer.Write(trafficFlowDistance);
		float4 trafficFlowDistance2 = m_TrafficFlowDistance1;
		writer.Write(trafficFlowDistance2);
		RoadFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.netInfoviewImprovements)
		{
			ref float4 trafficFlowDuration = ref m_TrafficFlowDuration0;
			reader.Read(out trafficFlowDuration);
			ref float4 trafficFlowDuration2 = ref m_TrafficFlowDuration1;
			reader.Read(out trafficFlowDuration2);
			ref float4 trafficFlowDistance = ref m_TrafficFlowDistance0;
			reader.Read(out trafficFlowDistance);
			ref float4 trafficFlowDistance2 = ref m_TrafficFlowDistance1;
			reader.Read(out trafficFlowDistance2);
		}
		else
		{
			ref float4 trafficFlowDuration3 = ref m_TrafficFlowDuration0;
			reader.Read(out trafficFlowDuration3);
			ref float4 trafficFlowDistance3 = ref m_TrafficFlowDistance0;
			reader.Read(out trafficFlowDistance3);
			if (reader.context.version < Version.trafficFlowFixes)
			{
				float4 @float = m_TrafficFlowDuration0 + 1f;
				m_TrafficFlowDistance0 *= 0.01f;
				m_TrafficFlowDuration0 = math.select(m_TrafficFlowDistance0 / @float, 0f, @float <= 0f);
			}
			m_TrafficFlowDuration0 *= 0.5f;
			m_TrafficFlowDistance0 *= 0.5f;
			m_TrafficFlowDuration1 = m_TrafficFlowDuration0;
			m_TrafficFlowDistance1 = m_TrafficFlowDistance0;
		}
		if (reader.context.version >= Version.roadFlags)
		{
			reader.Read(out byte value);
			m_Flags = (RoadFlags)value;
		}
	}
}
