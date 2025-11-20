using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct TreeData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_WoodAmount;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_WoodAmount);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_WoodAmount);
	}
}
