using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct AtmosphereData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_AtmospherePrefab;

	public AtmosphereData(Entity prefab)
	{
		m_AtmospherePrefab = prefab;
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_AtmospherePrefab);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_AtmospherePrefab);
	}
}
