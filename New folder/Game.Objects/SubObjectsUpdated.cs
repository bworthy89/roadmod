using Unity.Entities;

namespace Game.Objects;

public struct SubObjectsUpdated : IComponentData, IQueryTypeParameter
{
	public Entity m_Owner;

	public SubObjectsUpdated(Entity owner)
	{
		m_Owner = owner;
	}
}
