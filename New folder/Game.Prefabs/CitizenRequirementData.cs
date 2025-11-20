using Unity.Entities;

namespace Game.Prefabs;

public struct CitizenRequirementData : IComponentData, IQueryTypeParameter
{
	public int m_MinimumPopulation;

	public int m_MinimumHappiness;
}
