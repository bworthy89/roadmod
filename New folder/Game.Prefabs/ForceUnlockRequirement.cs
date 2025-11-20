using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { typeof(UIGroupPrefab) })]
public class ForceUnlockRequirement : ComponentBase
{
	public PrefabBase m_Prefab;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ForceUnlockRequirementData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_Prefab != null && entityManager.World.GetExistingSystemManaged<PrefabSystem>().TryGetEntity(m_Prefab, out var entity2))
		{
			ForceUnlockRequirementData componentData = default(ForceUnlockRequirementData);
			componentData.m_Prefab = entity2;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
