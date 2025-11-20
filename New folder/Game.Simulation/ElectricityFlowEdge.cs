using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Simulation;

public struct ElectricityFlowEdge : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Index;

	public Entity m_Start;

	public Entity m_End;

	public int m_Capacity;

	public int m_Flow;

	public ElectricityFlowEdgeFlags m_Flags;

	public FlowDirection direction
	{
		get
		{
			return (FlowDirection)(m_Flags & ElectricityFlowEdgeFlags.ForwardBackward);
		}
		set
		{
			m_Flags &= ~ElectricityFlowEdgeFlags.ForwardBackward;
			m_Flags |= (ElectricityFlowEdgeFlags)value;
		}
	}

	public bool isBottleneck => (m_Flags & ElectricityFlowEdgeFlags.Bottleneck) != 0;

	public bool isBeyondBottleneck => (m_Flags & ElectricityFlowEdgeFlags.BeyondBottleneck) != 0;

	public bool isDisconnected => (m_Flags & ElectricityFlowEdgeFlags.Disconnected) != 0;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity start = m_Start;
		writer.Write(start);
		Entity end = m_End;
		writer.Write(end);
		int flow = m_Flow;
		writer.Write(flow);
		int capacity = m_Capacity;
		writer.Write(capacity);
		ElectricityFlowEdgeFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity start = ref m_Start;
		reader.Read(out start);
		ref Entity end = ref m_End;
		reader.Read(out end);
		ref int flow = ref m_Flow;
		reader.Read(out flow);
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		if (reader.context.version > Version.electricityImprovements2)
		{
			reader.Read(out byte value);
			m_Flags = (ElectricityFlowEdgeFlags)value;
		}
		else if (reader.context.version >= Version.electricityImprovements)
		{
			reader.Read(out int _);
			m_Flags = ElectricityFlowEdgeFlags.ForwardBackward;
		}
		else
		{
			m_Flags = ElectricityFlowEdgeFlags.ForwardBackward;
		}
	}
}
