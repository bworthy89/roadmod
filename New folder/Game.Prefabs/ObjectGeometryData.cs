using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct ObjectGeometryData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Bounds3 m_Bounds;

	public float3 m_Size;

	public float3 m_Pivot;

	public float3 m_LegSize;

	public float2 m_LegOffset;

	public GeometryFlags m_Flags;

	public int m_MinLod;

	public MeshLayer m_Layers;

	public ObjectRequirementFlags m_SubObjectMask;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 min = ref m_Bounds.min;
		reader.Read(out min);
		ref float3 max = ref m_Bounds.max;
		reader.Read(out max);
		ref float3 size = ref m_Size;
		reader.Read(out size);
		ref float3 pivot = ref m_Pivot;
		reader.Read(out pivot);
		ref float3 legSize = ref m_LegSize;
		reader.Read(out legSize);
		if (reader.context.format.Has(FormatTags.StandingLegOffset))
		{
			ref float2 legOffset = ref m_LegOffset;
			reader.Read(out legOffset);
		}
		reader.Read(out uint value);
		m_Flags = (GeometryFlags)value;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 min = m_Bounds.min;
		writer.Write(min);
		float3 max = m_Bounds.max;
		writer.Write(max);
		float3 size = m_Size;
		writer.Write(size);
		float3 pivot = m_Pivot;
		writer.Write(pivot);
		float3 legSize = m_LegSize;
		writer.Write(legSize);
		float2 legOffset = m_LegOffset;
		writer.Write(legOffset);
		GeometryFlags flags = m_Flags;
		writer.Write((uint)flags);
	}
}
