using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct CargoTransportStation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_WorkAmount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_WorkAmount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_WorkAmount);
	}
}
