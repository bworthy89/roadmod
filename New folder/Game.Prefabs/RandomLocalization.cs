using System;
using System.Collections.Generic;
using Game.Common;
using Game.SceneFlow;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Localization/", new Type[] { })]
public class RandomLocalization : Localization
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<LocalizationCount>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<RandomLocalizationIndex>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		int localizationCount = GetLocalizationCount();
		entityManager.GetBuffer<LocalizationCount>(entity).Add(new LocalizationCount
		{
			m_Count = localizationCount
		});
	}

	protected virtual int GetLocalizationCount()
	{
		return GetLocalizationIndexCount(base.prefab, m_LocalizationID);
	}

	public static int GetLocalizationIndexCount(PrefabBase prefab, string id)
	{
		int num = -1;
		if (id != null && GameManager.instance.localizationManager.activeDictionary.indexCounts.TryGetValue(id, out var value))
		{
			num = value;
		}
		if (num < 1)
		{
			ComponentBase.baseLog.WarnFormat(prefab, "Warning: localizationID {0} not found for {1}", id, prefab.name);
		}
		return num;
	}
}
