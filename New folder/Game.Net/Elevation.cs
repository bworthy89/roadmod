using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct Elevation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float2 m_Elevation;

	public Elevation(float2 elevation)
	{
		m_Elevation = elevation;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Elevation);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Elevation);
	}
}
