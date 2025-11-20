using Unity.Entities;

namespace Game.Prefabs;

public struct EventData : IComponentData, IQueryTypeParameter
{
	public EntityArchetype m_Archetype;

	public int m_ConcurrentLimit;
}
