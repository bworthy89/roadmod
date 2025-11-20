using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class StrictObjectBuiltRequirementPrefab : UnlockRequirementPrefab
{
	public PrefabBase m_Requirement;

	public int m_MinimumCount = 1;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_Requirement);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<StrictObjectBuiltRequirementData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
		Entity entity2 = existingSystemManaged.GetEntity(m_Requirement);
		StrictObjectBuiltRequirementData componentData = new StrictObjectBuiltRequirementData
		{
			m_Requirement = entity2,
			m_MinimumCount = m_MinimumCount
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
