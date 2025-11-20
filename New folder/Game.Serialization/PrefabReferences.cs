using Game.Prefabs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Serialization;

public struct PrefabReferences
{
	[ReadOnly]
	private ComponentLookup<PrefabData> m_PrefabData;

	[ReadOnly]
	private NativeArray<Entity> m_PrefabArray;

	private UnsafeList<bool> m_ReferencedPrefabs;

	private Entity m_LastPrefabIn;

	private Entity m_LastPrefabOut;

	private bool m_IsLoading;

	public PrefabReferences(NativeArray<Entity> prefabArray, UnsafeList<bool> referencedPrefabs, ComponentLookup<PrefabData> prefabData, bool isLoading)
	{
		m_PrefabArray = prefabArray;
		m_ReferencedPrefabs = referencedPrefabs;
		m_PrefabData = prefabData;
		m_LastPrefabIn = Entity.Null;
		m_LastPrefabOut = Entity.Null;
		m_IsLoading = isLoading;
	}

	public void SetDirty(Entity prefab)
	{
		if (!m_IsLoading)
		{
			PrefabData prefabData = m_PrefabData[prefab];
			if (prefabData.m_Index >= 0)
			{
				m_ReferencedPrefabs[prefabData.m_Index] = true;
			}
		}
	}

	public void Check(ref Entity prefab)
	{
		if (prefab != m_LastPrefabIn)
		{
			PrefabData prefabData = m_PrefabData[prefab];
			prefabData.m_Index = math.select(prefabData.m_Index, m_ReferencedPrefabs.Length + prefabData.m_Index, prefabData.m_Index < 0);
			m_LastPrefabIn = prefab;
			if (m_IsLoading)
			{
				m_LastPrefabOut = m_PrefabArray[prefabData.m_Index];
				if (m_LastPrefabOut == m_LastPrefabIn)
				{
					m_ReferencedPrefabs[prefabData.m_Index] = true;
				}
			}
			else
			{
				m_LastPrefabOut = prefab;
				m_ReferencedPrefabs[prefabData.m_Index] = true;
			}
		}
		prefab = m_LastPrefabOut;
	}

	public Entity Check(EntityManager entityManager, Entity prefab)
	{
		if (prefab == Entity.Null)
		{
			return Entity.Null;
		}
		if (m_IsLoading && entityManager.HasComponent<LoadedIndex>(prefab))
		{
			return prefab;
		}
		if (prefab != m_LastPrefabIn)
		{
			PrefabData componentData = entityManager.GetComponentData<PrefabData>(prefab);
			componentData.m_Index = math.select(componentData.m_Index, m_ReferencedPrefabs.Length + componentData.m_Index, componentData.m_Index < 0);
			m_LastPrefabIn = prefab;
			if (m_IsLoading)
			{
				m_LastPrefabOut = m_PrefabArray[componentData.m_Index];
				if (m_LastPrefabOut == m_LastPrefabIn)
				{
					m_ReferencedPrefabs[componentData.m_Index] = true;
				}
			}
			else
			{
				m_LastPrefabOut = prefab;
				m_ReferencedPrefabs[componentData.m_Index] = true;
			}
		}
		return m_LastPrefabOut;
	}
}
