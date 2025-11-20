using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct RouteNumber : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Number;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Number);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Number);
	}
}
