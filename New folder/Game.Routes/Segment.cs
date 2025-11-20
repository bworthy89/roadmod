using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct Segment : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Index;

	public Segment(int index)
	{
		m_Index = index;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Index);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Index);
	}
}
