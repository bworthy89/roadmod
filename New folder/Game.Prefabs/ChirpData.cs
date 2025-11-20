using Unity.Entities;

namespace Game.Prefabs;

public struct ChirpData : IComponentData, IQueryTypeParameter
{
	public EntityArchetype m_Archetype;

	public ChirpDataFlags m_Flags;

	public Entity m_ChirperAccount;
}
