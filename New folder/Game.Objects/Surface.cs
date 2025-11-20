using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Surface : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_Wetness;

	public byte m_SnowAmount;

	public byte m_AccumulatedWetness;

	public byte m_AccumulatedSnow;

	public byte m_Dirtyness;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte wetness = m_Wetness;
		writer.Write(wetness);
		byte snowAmount = m_SnowAmount;
		writer.Write(snowAmount);
		byte accumulatedWetness = m_AccumulatedWetness;
		writer.Write(accumulatedWetness);
		byte accumulatedSnow = m_AccumulatedSnow;
		writer.Write(accumulatedSnow);
		byte dirtyness = m_Dirtyness;
		writer.Write(dirtyness);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref byte wetness = ref m_Wetness;
		reader.Read(out wetness);
		ref byte snowAmount = ref m_SnowAmount;
		reader.Read(out snowAmount);
		ref byte accumulatedWetness = ref m_AccumulatedWetness;
		reader.Read(out accumulatedWetness);
		ref byte accumulatedSnow = ref m_AccumulatedSnow;
		reader.Read(out accumulatedSnow);
		ref byte dirtyness = ref m_Dirtyness;
		reader.Read(out dirtyness);
	}
}
