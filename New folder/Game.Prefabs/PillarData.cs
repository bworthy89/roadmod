using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PillarData : IComponentData, IQueryTypeParameter, ISerializable
{
	public PillarType m_Type;

	public Bounds1 m_OffsetRange;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		PillarType type = m_Type;
		writer.Write((int)type);
		float min = m_OffsetRange.min;
		writer.Write(min);
		float max = m_OffsetRange.max;
		writer.Write(max);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		ref float min = ref m_OffsetRange.min;
		reader.Read(out min);
		ref float max = ref m_OffsetRange.max;
		reader.Read(out max);
		m_Type = (PillarType)value;
	}
}
