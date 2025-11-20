using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.PSI.Common;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public static class PrefabUtils
{
	public static T[] ToArray<T>(HashSet<T> hashSet)
	{
		T[] array = new T[hashSet.Count];
		hashSet.CopyTo(array);
		return array;
	}

	public static bool HasUnlockedPrefab<T>(EntityManager entityManager, EntityQuery unlockQuery) where T : unmanaged
	{
		if (!unlockQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Unlock> nativeArray = unlockQuery.ToComponentDataArray<Unlock>(Allocator.TempJob);
			try
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (entityManager.HasComponent<T>(nativeArray[i].m_Prefab))
					{
						return true;
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
		return false;
	}

	public static bool HasUnlockedPrefabAll<T1, T2>(EntityManager entityManager, EntityQuery unlockQuery) where T1 : unmanaged where T2 : unmanaged
	{
		if (!unlockQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Unlock> nativeArray = unlockQuery.ToComponentDataArray<Unlock>(Allocator.TempJob);
			try
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (entityManager.HasComponent<T1>(nativeArray[i].m_Prefab) && entityManager.HasComponent<T2>(nativeArray[i].m_Prefab))
					{
						return true;
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
		return false;
	}

	public static bool HasUnlockedPrefabAny<T1, T2, T3, T4>(EntityManager entityManager, EntityQuery unlockQuery) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
	{
		if (!unlockQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Unlock> nativeArray = unlockQuery.ToComponentDataArray<Unlock>(Allocator.TempJob);
			try
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (entityManager.HasComponent<T1>(nativeArray[i].m_Prefab) || entityManager.HasComponent<T2>(nativeArray[i].m_Prefab) || entityManager.HasComponent<T3>(nativeArray[i].m_Prefab) || entityManager.HasComponent<T4>(nativeArray[i].m_Prefab))
					{
						return true;
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
		return false;
	}

	[CanBeNull]
	public static string GetContentPrerequisite(PrefabBase prefab)
	{
		if (prefab.TryGet<ContentPrerequisite>(out var component) && component.m_ContentPrerequisite.TryGet<DlcRequirement>(out var component2))
		{
			return PlatformManager.instance.GetDlcName(component2.m_Dlc);
		}
		return null;
	}
}
