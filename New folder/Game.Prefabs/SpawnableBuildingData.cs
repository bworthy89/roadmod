using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct SpawnableBuildingData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ZonePrefab;

	public byte m_Level;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity zonePrefab = m_ZonePrefab;
		writer.Write(zonePrefab);
		byte level = m_Level;
		writer.Write(level);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity zonePrefab = ref m_ZonePrefab;
		reader.Read(out zonePrefab);
		ref byte level = ref m_Level;
		reader.Read(out level);
	}
}
