using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Zones;

public struct CurvePosition : IComponentData, IQueryTypeParameter, ISerializable
{
	public float2 m_CurvePosition;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_CurvePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_CurvePosition);
	}
}
