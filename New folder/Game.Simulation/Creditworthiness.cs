using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct Creditworthiness : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Amount;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Amount);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Amount);
	}
}
