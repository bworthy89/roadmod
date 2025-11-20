using Unity.Entities;

namespace Game.Prefabs;

public struct ForceUnlockRequirementData : IComponentData, IQueryTypeParameter
{
	public Entity m_Prefab;
}
