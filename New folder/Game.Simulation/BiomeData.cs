using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct BiomeData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_BiomePrefab;

	public BiomeData(Entity prefab)
	{
		m_BiomePrefab = prefab;
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_BiomePrefab);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_BiomePrefab);
	}
}
