using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Prefabs;

[CreateAssetMenu(fileName = "AssetCollection", menuName = "Colossal/Prefabs/AssetCollection", order = 0)]
public class AssetCollection : ScriptableObject
{
	public bool isActive = true;

	[ContextMenuItem("Sort Assets Alphabetically", "SortAssets")]
	public List<PrefabBase> m_Prefabs;

	public List<AssetCollection> m_Collections;

	public int Count => m_Prefabs.Count;

	public void AddPrefabsTo(PrefabSystem prefabSystem)
	{
		if (!isActive)
		{
			return;
		}
		foreach (PrefabBase prefab in m_Prefabs)
		{
			prefabSystem.AddPrefab(prefab, base.name);
		}
		foreach (AssetCollection collection in m_Collections)
		{
			collection.AddPrefabsTo(prefabSystem);
		}
	}

	public void SortAssets()
	{
		m_Prefabs.Sort((PrefabBase a, PrefabBase b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
	}
}
