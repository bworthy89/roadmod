using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ContentPrerequisiteData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ContentPrerequisite;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_ContentPrerequisite);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_ContentPrerequisite);
	}
}
