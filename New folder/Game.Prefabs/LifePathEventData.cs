using Unity.Entities;

namespace Game.Prefabs;

public struct LifePathEventData : IComponentData, IQueryTypeParameter
{
	public EntityArchetype m_ChirpArchetype;

	public LifePathEventType m_EventType;

	public bool m_IsChirp;
}
