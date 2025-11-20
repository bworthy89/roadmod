using Unity.Entities;

namespace Game.Prefabs;

public struct StrictObjectBuiltRequirementData : IComponentData, IQueryTypeParameter
{
	public Entity m_Requirement;

	public int m_MinimumCount;
}
