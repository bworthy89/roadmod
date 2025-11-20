using Game.Economy;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct ResourcePrefabs
{
	private NativeArray<Entity> m_ResourcePrefabs;

	public Entity this[Resource resource]
	{
		get
		{
			int resourceIndex = EconomyUtils.GetResourceIndex(resource);
			if (resourceIndex >= 0 && resourceIndex < m_ResourcePrefabs.Length)
			{
				return m_ResourcePrefabs[resourceIndex];
			}
			return Entity.Null;
		}
	}

	public ResourcePrefabs(NativeArray<Entity> resourcePrefabs)
	{
		m_ResourcePrefabs = resourcePrefabs;
	}
}
