using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ElectricityNodeConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ElectricityNode;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_ElectricityNode);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_ElectricityNode);
	}
}
