using Unity.Entities;

namespace Game.Prefabs;

public struct EffectData : IComponentData, IQueryTypeParameter
{
	public EntityArchetype m_Archetype;

	public EffectCondition m_Flags;

	public bool m_OwnerCulling;
}
