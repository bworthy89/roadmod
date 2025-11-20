using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct PopulationCell : IPopulationCell, IStrideSerializable, ISerializable
{
	public float m_Population;

	public float Get()
	{
		return m_Population;
	}

	public void Add(float amount)
	{
		m_Population += amount;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Population);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Population);
	}

	public int GetStride(Context context)
	{
		return 4;
	}
}
