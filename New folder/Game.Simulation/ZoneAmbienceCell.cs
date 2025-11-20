using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct ZoneAmbienceCell : IStrideSerializable, ISerializable
{
	public ZoneAmbiences m_Accumulator;

	public ZoneAmbiences m_Value;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ZoneAmbiences accumulator = m_Accumulator;
		writer.Write(accumulator);
		ZoneAmbiences value = m_Value;
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref ZoneAmbiences accumulator = ref m_Accumulator;
		reader.Read(out accumulator);
		ref ZoneAmbiences value = ref m_Value;
		reader.Read(out value);
	}

	public int GetStride(Context context)
	{
		return m_Accumulator.GetStride(context) + m_Value.GetStride(context);
	}
}
