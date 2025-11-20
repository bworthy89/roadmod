using Unity.Entities;

namespace Game.Prefabs;

public struct ArchetypeData : IComponentData, IQueryTypeParameter
{
	public EntityArchetype m_Archetype;
}
