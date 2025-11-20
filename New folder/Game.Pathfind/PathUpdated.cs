using Unity.Entities;

namespace Game.Pathfind;

public struct PathUpdated : IComponentData, IQueryTypeParameter
{
	public Entity m_Owner;

	public PathEventData m_Data;

	public PathUpdated(Entity owner, PathEventData data)
	{
		m_Owner = owner;
		m_Data = data;
	}
}
