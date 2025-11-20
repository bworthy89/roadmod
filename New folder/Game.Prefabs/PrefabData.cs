using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PrefabData : IComponentData, IQueryTypeParameter, IEnableableComponent, ISerializable, ISerializeAsEnabled
{
	public int m_Index;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Index);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Index);
	}
}
