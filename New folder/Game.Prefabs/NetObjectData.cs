using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct NetObjectData : IComponentData, IQueryTypeParameter, ISerializable
{
	public CompositionFlags m_CompositionFlags;

	public RoadTypes m_RequireRoad;

	public RoadTypes m_RoadPassThrough;

	public TrackTypes m_TrackPassThrough;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_CompositionFlags);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_CompositionFlags);
	}
}
