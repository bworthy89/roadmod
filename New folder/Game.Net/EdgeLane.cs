using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct EdgeLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public float2 m_EdgeDelta;

	public byte m_ConnectedStartCount;

	public byte m_ConnectedEndCount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		bool3 @bool = m_EdgeDelta.x == new float3(0f, 0.5f, 1f);
		bool3 bool2 = m_EdgeDelta.y == new float3(0f, 0.5f, 1f);
		int3 @int = math.select(0, new int3(1, 2, 3), @bool.xyx & bool2.yzz);
		int3 int2 = math.select(0, new int3(4, 5, 6), @bool.zyz & bool2.yxx);
		byte b = (byte)math.csum(@int + int2);
		writer.Write(b);
		if (b == 0)
		{
			float2 edgeDelta = m_EdgeDelta;
			writer.Write(edgeDelta);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.saveOptimizations)
		{
			reader.Read(out byte value);
			switch (value)
			{
			case 1:
				m_EdgeDelta = new float2(0f, 0.5f);
				break;
			case 2:
				m_EdgeDelta = new float2(0.5f, 1f);
				break;
			case 3:
				m_EdgeDelta = new float2(0f, 1f);
				break;
			case 4:
				m_EdgeDelta = new float2(1f, 0.5f);
				break;
			case 5:
				m_EdgeDelta = new float2(0.5f, 0f);
				break;
			case 6:
				m_EdgeDelta = new float2(1f, 0f);
				break;
			default:
			{
				ref float2 edgeDelta = ref m_EdgeDelta;
				reader.Read(out edgeDelta);
				break;
			}
			}
		}
		else
		{
			ref float2 edgeDelta2 = ref m_EdgeDelta;
			reader.Read(out edgeDelta2);
		}
	}
}
