using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class TransportRequirementPrefab : UnlockRequirementPrefab
{
	public BuildingPrefab m_BuildingPrefab;

	public TransportType m_TransportType;

	public int m_FilterID;

	public int m_MinimumTransportedPassenger;

	public int m_MinimumTransportedCargo;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		if (m_BuildingPrefab != null)
		{
			prefabs.Add(m_BuildingPrefab);
		}
		base.GetDependencies(prefabs);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TransportRequirementData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
		TransportRequirementData componentData = default(TransportRequirementData);
		if (m_BuildingPrefab != null)
		{
			componentData.m_BuildingPrefab = existingSystemManaged.GetEntity(m_BuildingPrefab);
		}
		componentData.m_TransportType = m_TransportType;
		componentData.m_FilterID = m_FilterID;
		componentData.m_MinimumTransportedCargo = m_MinimumTransportedCargo;
		componentData.m_MinimumTransportedPassenger = m_MinimumTransportedPassenger;
		entityManager.SetComponentData(entity, componentData);
	}
}
