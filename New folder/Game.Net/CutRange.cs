using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(1)]
public struct CutRange : IBufferElementData, ISerializable
{
	public Bounds1 m_CurveDelta;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float min = m_CurveDelta.min;
		writer.Write(min);
		float max = m_CurveDelta.max;
		writer.Write(max);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float min = ref m_CurveDelta.min;
		reader.Read(out min);
		ref float max = ref m_CurveDelta.max;
		reader.Read(out max);
	}
}
