using Unity.Entities;

namespace Game.Prefabs;

public struct PopulationVictoryConfigurationData : IComponentData, IQueryTypeParameter
{
	public int m_populationGoal;
}
