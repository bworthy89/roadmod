using Colossal.Serialization.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Areas;

[InternalBufferCapacity(4)]
public struct Node : IBufferElementData, IStrideSerializable, ISerializable
{
	public float3 m_Position;

	public float m_Elevation;

	public Node(float3 position, float elevation)
	{
		m_Position = position;
		m_Elevation = elevation;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		float elevation = m_Elevation;
		writer.Write(elevation);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		if (reader.context.version >= Version.laneElevation)
		{
			ref float elevation = ref m_Elevation;
			reader.Read(out elevation);
		}
	}

	public int GetStride(Context context)
	{
		return UnsafeUtility.SizeOf<float3>() + 4;
	}
}
