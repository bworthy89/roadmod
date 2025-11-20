using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct EmergencyGenerator : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Production;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Production);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Production);
	}
}
