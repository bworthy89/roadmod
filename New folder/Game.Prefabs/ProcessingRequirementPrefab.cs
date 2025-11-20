using System;
using System.Collections.Generic;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class ProcessingRequirementPrefab : UnlockRequirementPrefab
{
	public ResourceInEditor m_ResourceType;

	public int m_MinimumProducedAmount = 10000;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ProcessingRequirementData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
		entityManager.SetComponentData(entity, new ProcessingRequirementData
		{
			m_ResourceType = EconomyUtils.GetResource(m_ResourceType),
			m_MinimumProducedAmount = m_MinimumProducedAmount
		});
	}
}
