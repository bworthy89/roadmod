using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Routes;

public struct Position : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Position;

	public Position(float3 position)
	{
		m_Position = position;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Position);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Position);
	}
}
