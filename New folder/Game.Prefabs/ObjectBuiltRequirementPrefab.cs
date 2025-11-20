using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class ObjectBuiltRequirementPrefab : UnlockRequirementPrefab
{
	public int m_MinimumCount = 1;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ObjectBuiltRequirementData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
		ObjectBuiltRequirementData componentData = new ObjectBuiltRequirementData
		{
			m_MinimumCount = m_MinimumCount
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
