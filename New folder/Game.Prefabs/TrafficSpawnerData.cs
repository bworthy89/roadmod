using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct TrafficSpawnerData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_SpawnRate;

	public RoadTypes m_RoadType;

	public TrackTypes m_TrackType;

	public bool m_NoSlowVehicles;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float spawnRate = m_SpawnRate;
		writer.Write(spawnRate);
		bool noSlowVehicles = m_NoSlowVehicles;
		writer.Write(noSlowVehicles);
		RoadTypes roadType = m_RoadType;
		writer.Write((byte)roadType);
		TrackTypes trackType = m_TrackType;
		writer.Write((byte)trackType);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float spawnRate = ref m_SpawnRate;
		reader.Read(out spawnRate);
		ref bool noSlowVehicles = ref m_NoSlowVehicles;
		reader.Read(out noSlowVehicles);
		reader.Read(out byte value);
		reader.Read(out byte value2);
		m_RoadType = (RoadTypes)value;
		m_TrackType = (TrackTypes)value2;
	}
}
