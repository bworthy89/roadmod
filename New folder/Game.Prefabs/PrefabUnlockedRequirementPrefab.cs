using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class PrefabUnlockedRequirementPrefab : UnlockRequirementPrefab
{
	public PrefabBase[] m_RequiredPrefabs;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PrefabUnlockedRequirement>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<PrefabUnlockedRequirement> buffer = entityManager.GetBuffer<PrefabUnlockedRequirement>(entity);
		PrefabBase[] requiredPrefabs = m_RequiredPrefabs;
		foreach (PrefabBase prefabBase in requiredPrefabs)
		{
			Entity entity2 = existingSystemManaged.GetEntity(prefabBase);
			buffer.Add(new PrefabUnlockedRequirement
			{
				m_Requirement = entity2
			});
		}
	}
}
