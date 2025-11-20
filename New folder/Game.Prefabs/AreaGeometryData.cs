using Colossal.Serialization.Entities;
using Game.Areas;
using Unity.Entities;

namespace Game.Prefabs;

public struct AreaGeometryData : IComponentData, IQueryTypeParameter, ISerializable
{
	public AreaType m_Type;

	public GeometryFlags m_Flags;

	public float m_SnapDistance;

	public float m_MaxHeight;

	public float m_LodBias;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)m_Type;
		writer.Write(value);
		GeometryFlags flags = m_Flags;
		writer.Write((uint)flags);
		float snapDistance = m_SnapDistance;
		writer.Write(snapDistance);
		float maxHeight = m_MaxHeight;
		writer.Write(maxHeight);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		reader.Read(out uint value2);
		ref float snapDistance = ref m_SnapDistance;
		reader.Read(out snapDistance);
		ref float maxHeight = ref m_MaxHeight;
		reader.Read(out maxHeight);
		m_Type = (AreaType)value;
		m_Flags = (GeometryFlags)value2;
	}
}
