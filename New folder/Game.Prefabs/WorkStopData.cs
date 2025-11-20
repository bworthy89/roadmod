using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct WorkStopData : IComponentData, IQueryTypeParameter, ISerializable
{
	public bool m_WorkLocation;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_WorkLocation);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_WorkLocation);
	}
}
