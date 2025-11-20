using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ServiceObjectData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Service;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Service);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Service);
	}
}
