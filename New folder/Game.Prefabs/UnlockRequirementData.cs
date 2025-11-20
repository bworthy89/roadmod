using Unity.Entities;

namespace Game.Prefabs;

public struct UnlockRequirementData : IComponentData, IQueryTypeParameter
{
	public int m_Progress;
}
