using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct NetZoneData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_BlockPrefab;

	public NetZoneData(Entity blockPrefab)
	{
		m_BlockPrefab = blockPrefab;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_BlockPrefab);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_BlockPrefab);
	}
}
