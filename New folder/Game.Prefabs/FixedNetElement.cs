using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct FixedNetElement : IBufferElementData, ISerializable
{
	public Bounds1 m_LengthRange;

	public int2 m_CountRange;

	public CompositionFlags m_SetState;

	public CompositionFlags m_UnsetState;

	public FixedNetFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float min = m_LengthRange.min;
		writer.Write(min);
		float max = m_LengthRange.max;
		writer.Write(max);
		int2 countRange = m_CountRange;
		writer.Write(countRange);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float min = ref m_LengthRange.min;
		reader.Read(out min);
		ref float max = ref m_LengthRange.max;
		reader.Read(out max);
		ref int2 countRange = ref m_CountRange;
		reader.Read(out countRange);
	}
}
