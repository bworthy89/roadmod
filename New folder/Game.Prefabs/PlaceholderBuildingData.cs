using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PlaceholderBuildingData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ZonePrefab;

	public BuildingType m_Type;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity zonePrefab = m_ZonePrefab;
		writer.Write(zonePrefab);
		BuildingType type = m_Type;
		writer.Write((int)type);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity zonePrefab = ref m_ZonePrefab;
		reader.Read(out zonePrefab);
		reader.Read(out int value);
		m_Type = (BuildingType)value;
	}
}
