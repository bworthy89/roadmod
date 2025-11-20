using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct StorageAreaData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Resource m_Resources;

	public int m_Capacity;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Resource resources = m_Resources;
		writer.Write((ulong)resources);
		int capacity = m_Capacity;
		writer.Write(capacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out ulong value);
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		m_Resources = (Resource)value;
	}
}
