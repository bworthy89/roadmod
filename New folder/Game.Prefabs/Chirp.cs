using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[]
{
	typeof(TriggerPrefab),
	typeof(StatisticTriggerPrefab)
})]
public class Chirp : ComponentBase
{
	[Tooltip("When the trigger happens, one of these chirps will be selected randomly")]
	public PrefabBase[] m_Chirps;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TriggerChirpData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<TriggerChirpData> buffer = entityManager.GetBuffer<TriggerChirpData>(entity);
		if (m_Chirps == null || m_Chirps.Length == 0)
		{
			return;
		}
		PrefabBase[] chirps = m_Chirps;
		foreach (PrefabBase prefabBase in chirps)
		{
			if (prefabBase != null)
			{
				buffer.Add(new TriggerChirpData
				{
					m_Chirp = existingSystemManaged.GetEntity(prefabBase)
				});
			}
		}
	}
}
