using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct HearseData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_CorpseCapacity;

	public HearseData(int corpseCapacity)
	{
		m_CorpseCapacity = corpseCapacity;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_CorpseCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_CorpseCapacity);
	}
}
