using Unity.Entities;

namespace Game.Prefabs;

public struct PrefabUnlockedRequirement : IBufferElementData
{
	public Entity m_Requirement;
}
