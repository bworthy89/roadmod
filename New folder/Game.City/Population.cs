using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct Population : IComponentData, IQueryTypeParameter, IDefaultSerializable, ISerializable
{
	public int m_Population;

	public int m_PopulationWithMoveIn;

	public int m_AverageHappiness;

	public int m_AverageHealth;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int population = ref m_Population;
		reader.Read(out population);
		ref int populationWithMoveIn = ref m_PopulationWithMoveIn;
		reader.Read(out populationWithMoveIn);
		ref int averageHappiness = ref m_AverageHappiness;
		reader.Read(out averageHappiness);
		if (reader.context.version >= Version.averageHealth)
		{
			ref int averageHealth = ref m_AverageHealth;
			reader.Read(out averageHealth);
		}
		else
		{
			m_AverageHealth = 50;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int population = m_Population;
		writer.Write(population);
		int populationWithMoveIn = m_PopulationWithMoveIn;
		writer.Write(populationWithMoveIn);
		int averageHappiness = m_AverageHappiness;
		writer.Write(averageHappiness);
		int averageHealth = m_AverageHealth;
		writer.Write(averageHealth);
	}

	public void SetDefaults(Context context)
	{
		m_Population = 0;
		m_PopulationWithMoveIn = 0;
		m_AverageHappiness = 50;
		m_AverageHealth = 50;
	}
}
