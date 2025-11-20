using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct Odometer : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Distance;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Distance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Distance);
	}
}
