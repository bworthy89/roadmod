using Unity.Entities;

namespace Game.Pathfind;

public struct CoverageUpdated : IComponentData, IQueryTypeParameter
{
	public Entity m_Owner;

	public PathEventData m_Data;

	public CoverageUpdated(Entity owner, PathEventData data)
	{
		m_Owner = owner;
		m_Data = data;
	}
}
