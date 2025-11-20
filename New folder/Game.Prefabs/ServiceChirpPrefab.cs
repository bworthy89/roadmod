using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[] { })]
public class ServiceChirpPrefab : ChirpPrefab
{
	public PrefabBase m_Account;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Account != null)
		{
			prefabs.Add(m_Account);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ServiceChirpData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		if (m_Account != null)
		{
			Entity entity2 = existingSystemManaged.GetEntity(m_Account);
			entityManager.SetComponentData(entity, new ServiceChirpData
			{
				m_Account = entity2
			});
		}
	}
}
