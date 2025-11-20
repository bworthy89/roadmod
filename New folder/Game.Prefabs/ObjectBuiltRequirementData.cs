using Unity.Entities;

namespace Game.Prefabs;

public struct ObjectBuiltRequirementData : IComponentData, IQueryTypeParameter
{
	public int m_MinimumCount;
}
