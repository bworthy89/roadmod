using System;
using System.Collections.Generic;
using System.Threading;
using Colossal;
using Colossal.Logging;
using Game.Prefabs;
using UnityEngine;

namespace Game.SceneFlow;

[CreateAssetMenu(fileName = "AssetLibrary", menuName = "Colossal/Prefabs/AssetLibrary", order = 10)]
public class AssetLibrary : ScriptableObject
{
	public List<AssetCollection> m_Collections;

	private int m_AssetCount;

	private int m_ProgressCount;

	public float progress
	{
		get
		{
			if (m_AssetCount <= 0)
			{
				return 0f;
			}
			return (float)m_ProgressCount / (float)(m_AssetCount - 1);
		}
	}

	public void Load(PrefabSystem prefabSystem, CancellationToken token)
	{
		ILog log = LogManager.GetLogger("SceneFlow");
		prefabSystem.UpdateAvailabilityCache();
		m_ProgressCount = 0;
		m_AssetCount = GetCount();
		using (PerformanceCounter.Start(delegate(TimeSpan t)
		{
			log.InfoFormat("Added {0}/{1} explicitly referenced prefabs in {2}s", m_ProgressCount, m_AssetCount, t.TotalSeconds);
		}))
		{
			foreach (AssetCollection collection in m_Collections)
			{
				token.ThrowIfCancellationRequested();
				m_ProgressCount += collection.Count;
				collection.AddPrefabsTo(prefabSystem);
			}
		}
	}

	private int GetCount()
	{
		int num = 0;
		foreach (AssetCollection collection in m_Collections)
		{
			num += collection.Count;
		}
		return num;
	}
}
