using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct NodeLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public float2 m_WidthOffset;

	public NodeLaneFlags m_Flags;

	public byte m_SharedStartCount;

	public byte m_SharedEndCount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		NodeLaneFlags flags = m_Flags;
		writer.Write((byte)flags);
		if ((m_Flags & NodeLaneFlags.StartWidthOffset) != 0)
		{
			float x = m_WidthOffset.x;
			writer.Write(x);
		}
		if ((m_Flags & NodeLaneFlags.EndWidthOffset) != 0)
		{
			float y = m_WidthOffset.y;
			writer.Write(y);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.saveOptimizations)
		{
			reader.Read(out byte value);
			m_Flags = (NodeLaneFlags)value;
			if ((m_Flags & NodeLaneFlags.StartWidthOffset) != 0)
			{
				ref float x = ref m_WidthOffset.x;
				reader.Read(out x);
			}
			if ((m_Flags & NodeLaneFlags.EndWidthOffset) != 0)
			{
				ref float y = ref m_WidthOffset.y;
				reader.Read(out y);
			}
		}
		else
		{
			ref float2 widthOffset = ref m_WidthOffset;
			reader.Read(out widthOffset);
		}
	}
}
