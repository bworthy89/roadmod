using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct TrackLaneData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_FallbackPrefab;

	public Entity m_EndObjectPrefab;

	public TrackTypes m_TrackTypes;

	public float m_MaxCurviness;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_TrackTypes);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_TrackTypes = (TrackTypes)value;
		m_MaxCurviness = float.MaxValue;
	}
}
