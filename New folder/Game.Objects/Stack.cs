using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Stack : IComponentData, IQueryTypeParameter, ISerializable
{
	public Bounds1 m_Range;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float min = m_Range.min;
		writer.Write(min);
		float max = m_Range.max;
		writer.Write(max);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float min = ref m_Range.min;
		reader.Read(out min);
		ref float max = ref m_Range.max;
		reader.Read(out max);
	}
}
