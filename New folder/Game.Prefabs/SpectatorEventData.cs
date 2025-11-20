using Unity.Entities;

namespace Game.Prefabs;

public struct SpectatorEventData : IComponentData, IQueryTypeParameter
{
	public EventTargetType m_RandomSiteType;

	public float m_PreparationDuration;

	public float m_ActiveDuration;

	public float m_TerminationDuration;
}
