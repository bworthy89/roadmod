using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct NetGeometryData : IComponentData, IQueryTypeParameter, ISerializable
{
	public EntityArchetype m_NodeCompositionArchetype;

	public EntityArchetype m_EdgeCompositionArchetype;

	public Entity m_AggregateType;

	public Entity m_StyleType;

	public Bounds1 m_DefaultHeightRange;

	public Bounds1 m_ElevatedHeightRange;

	public Bounds1 m_DefaultSurfaceHeight;

	public Bounds1 m_EdgeLengthRange;

	public Layer m_MergeLayers;

	public Layer m_IntersectLayers;

	public GeometryFlags m_Flags;

	public float m_DefaultWidth;

	public float m_ElevatedWidth;

	public float m_ElevatedLength;

	public float m_MinNodeOffset;

	public float m_ElevationLimit;

	public float m_MaxSlopeSteepness;

	public float m_Hanging;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float min = m_DefaultHeightRange.min;
		writer.Write(min);
		float max = m_DefaultHeightRange.max;
		writer.Write(max);
		float min2 = m_ElevatedHeightRange.min;
		writer.Write(min2);
		float max2 = m_ElevatedHeightRange.max;
		writer.Write(max2);
		float min3 = m_DefaultSurfaceHeight.min;
		writer.Write(min3);
		float max3 = m_DefaultSurfaceHeight.max;
		writer.Write(max3);
		float min4 = m_EdgeLengthRange.min;
		writer.Write(min4);
		float max4 = m_EdgeLengthRange.max;
		writer.Write(max4);
		Layer mergeLayers = m_MergeLayers;
		writer.Write((uint)mergeLayers);
		Layer intersectLayers = m_IntersectLayers;
		writer.Write((uint)intersectLayers);
		GeometryFlags flags = m_Flags;
		writer.Write((uint)flags);
		float defaultWidth = m_DefaultWidth;
		writer.Write(defaultWidth);
		float elevatedWidth = m_ElevatedWidth;
		writer.Write(elevatedWidth);
		float minNodeOffset = m_MinNodeOffset;
		writer.Write(minNodeOffset);
		float elevationLimit = m_ElevationLimit;
		writer.Write(elevationLimit);
		float maxSlopeSteepness = m_MaxSlopeSteepness;
		writer.Write(maxSlopeSteepness);
		float hanging = m_Hanging;
		writer.Write(hanging);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float min = ref m_DefaultHeightRange.min;
		reader.Read(out min);
		ref float max = ref m_DefaultHeightRange.max;
		reader.Read(out max);
		ref float min2 = ref m_ElevatedHeightRange.min;
		reader.Read(out min2);
		ref float max2 = ref m_ElevatedHeightRange.max;
		reader.Read(out max2);
		ref float min3 = ref m_DefaultSurfaceHeight.min;
		reader.Read(out min3);
		ref float max3 = ref m_DefaultSurfaceHeight.max;
		reader.Read(out max3);
		ref float min4 = ref m_EdgeLengthRange.min;
		reader.Read(out min4);
		ref float max4 = ref m_EdgeLengthRange.max;
		reader.Read(out max4);
		reader.Read(out uint value);
		reader.Read(out uint value2);
		reader.Read(out uint value3);
		ref float defaultWidth = ref m_DefaultWidth;
		reader.Read(out defaultWidth);
		ref float elevatedWidth = ref m_ElevatedWidth;
		reader.Read(out elevatedWidth);
		ref float minNodeOffset = ref m_MinNodeOffset;
		reader.Read(out minNodeOffset);
		ref float elevationLimit = ref m_ElevationLimit;
		reader.Read(out elevationLimit);
		ref float maxSlopeSteepness = ref m_MaxSlopeSteepness;
		reader.Read(out maxSlopeSteepness);
		ref float hanging = ref m_Hanging;
		reader.Read(out hanging);
		m_MergeLayers = (Layer)value;
		m_IntersectLayers = (Layer)value2;
		m_Flags = (GeometryFlags)value3;
		m_ElevatedLength = m_EdgeLengthRange.max;
	}
}
