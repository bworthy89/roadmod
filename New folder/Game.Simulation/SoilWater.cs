using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct SoilWater : IStrideSerializable, ISerializable
{
	public float m_Surface;

	public short m_Amount;

	public short m_Max;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float surface = m_Surface;
		writer.Write(surface);
		short amount = m_Amount;
		writer.Write(amount);
		short max = m_Max;
		writer.Write(max);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float surface = ref m_Surface;
		reader.Read(out surface);
		ref short amount = ref m_Amount;
		reader.Read(out amount);
		ref short max = ref m_Max;
		reader.Read(out max);
	}

	public int GetStride(Context context)
	{
		return 8;
	}
}
