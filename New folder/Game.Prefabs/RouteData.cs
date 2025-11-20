using Colossal.Serialization.Entities;
using Game.Routes;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct RouteData : IComponentData, IQueryTypeParameter, ISerializable
{
	public EntityArchetype m_RouteArchetype;

	public EntityArchetype m_WaypointArchetype;

	public EntityArchetype m_ConnectedArchetype;

	public EntityArchetype m_SegmentArchetype;

	public float m_SnapDistance;

	public RouteType m_Type;

	public Color32 m_Color;

	public float m_Width;

	public float m_SegmentLength;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)m_Type;
		writer.Write(value);
		float width = m_Width;
		writer.Write(width);
		float segmentLength = m_SegmentLength;
		writer.Write(segmentLength);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		ref float width = ref m_Width;
		reader.Read(out width);
		ref float segmentLength = ref m_SegmentLength;
		reader.Read(out segmentLength);
		m_Type = (RouteType)value;
		m_Color = new Color32(128, 128, 128, byte.MaxValue);
		m_SnapDistance = m_Width;
	}
}
