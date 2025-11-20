using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[RequireComponent(typeof(UnlockableBase))]
[ComponentMenu("Prefabs/Unlocking/", new Type[]
{
	typeof(TutorialPrefab),
	typeof(TutorialPhasePrefab),
	typeof(TutorialTriggerPrefabBase),
	typeof(TutorialListPrefab)
})]
public class ForceUIGroupUnlock : ComponentBase
{
	[Tooltip("UIGroups listed here will unlock whenever this prefab unlocks, regardless if their own unlock requirements have been met.")]
	public UIGroupPrefab[] m_Unlocks;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ForceUIGroupUnlockData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<ForceUIGroupUnlockData> buffer = entityManager.GetBuffer<ForceUIGroupUnlockData>(entity);
		for (int i = 0; i < m_Unlocks.Length; i++)
		{
			Entity entity2 = existingSystemManaged.GetEntity(m_Unlocks[i]);
			buffer.Add(new ForceUIGroupUnlockData
			{
				m_Entity = entity2
			});
		}
	}
}
