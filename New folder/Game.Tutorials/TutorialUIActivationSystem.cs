using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialUIActivationSystem : GameSystemBase, ITutorialUIActivationSystem
{
	protected EntityCommandBufferSystem m_BarrierSystem;

	private readonly Dictionary<string, List<Entity>> m_TutorialMap = new Dictionary<string, List<Entity>>();

	private readonly List<string> m_ActiveTags = new List<string>();

	private EntityQuery m_TutorialQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BarrierSystem = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_TutorialQuery = GetEntityQuery(ComponentType.ReadOnly<UIActivationData>(), ComponentType.ReadOnly<PrefabData>());
		RebuildTutorialMap();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		RebuildTutorialMap();
		m_ActiveTags.Clear();
	}

	private void RebuildTutorialMap()
	{
		m_TutorialMap.Clear();
		if (m_TutorialQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<Entity> nativeArray = m_TutorialQuery.ToEntityArray(Allocator.TempJob);
		PrefabSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity item = nativeArray[i];
			string[] array = orCreateSystemManaged.GetPrefab<TutorialPrefab>(nativeArray[i]).GetComponent<TutorialUIActivation>().m_UITagProvider?.uiTag?.Split('|');
			if (array == null)
			{
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				string key = array[j].Trim();
				if (!m_TutorialMap.ContainsKey(key))
				{
					m_TutorialMap[key] = new List<Entity>();
				}
				if (!m_TutorialMap[key].Contains(item))
				{
					m_TutorialMap[key].Add(item);
				}
			}
		}
		nativeArray.Dispose();
	}

	public void SetTag(string tag, bool active)
	{
		if (m_TutorialMap.ContainsKey(tag))
		{
			m_ActiveTags.Remove(tag);
			if (active)
			{
				m_ActiveTags.Add(tag);
			}
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ActiveTags.Count <= 0)
		{
			return;
		}
		EntityCommandBuffer entityCommandBuffer = m_BarrierSystem.CreateCommandBuffer();
		foreach (string item in m_ActiveTags)
		{
			if (!m_TutorialMap.TryGetValue(item, out var value))
			{
				continue;
			}
			foreach (Entity item2 in value)
			{
				if (!base.EntityManager.HasComponent<TutorialCompleted>(item2))
				{
					base.EntityManager.AddComponent<TutorialActivated>(item2);
					if (!base.EntityManager.GetComponentData<UIActivationData>(item2).m_CanDeactivate)
					{
						entityCommandBuffer.AddComponent<ForceActivation>(item2);
					}
				}
			}
		}
	}

	[Preserve]
	public TutorialUIActivationSystem()
	{
	}
}
