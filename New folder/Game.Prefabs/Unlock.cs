using Unity.Entities;

namespace Game.Prefabs;

public struct Unlock : IComponentData, IQueryTypeParameter
{
	public Entity m_Prefab;

	public Unlock(Entity prefab)
	{
		m_Prefab = prefab;
	}
}
