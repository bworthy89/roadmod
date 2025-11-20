using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct ProcessingRequirementData : IComponentData, IQueryTypeParameter
{
	public Resource m_ResourceType;

	public int m_MinimumProducedAmount;
}
