using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class CitizenRequirementPrefab : UnlockRequirementPrefab
{
	public int m_MinimumPopulation = 100;

	public int m_MinimumHappiness;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CitizenRequirementData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
		entityManager.SetComponentData(entity, new CitizenRequirementData
		{
			m_MinimumPopulation = m_MinimumPopulation,
			m_MinimumHappiness = m_MinimumHappiness
		});
	}
}
