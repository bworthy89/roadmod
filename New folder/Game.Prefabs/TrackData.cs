using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct TrackData : IComponentData, IQueryTypeParameter, ISerializable
{
	public TrackTypes m_TrackType;

	public float m_SpeedLimit;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		TrackTypes trackType = m_TrackType;
		writer.Write((byte)trackType);
		float speedLimit = m_SpeedLimit;
		writer.Write(speedLimit);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref float speedLimit = ref m_SpeedLimit;
		reader.Read(out speedLimit);
		m_TrackType = (TrackTypes)value;
	}
}
