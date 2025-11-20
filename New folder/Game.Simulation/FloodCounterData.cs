using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct FloodCounterData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_FloodCounter;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_FloodCounter);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_FloodCounter);
	}
}
