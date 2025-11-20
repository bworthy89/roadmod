using System;
using System.Collections.Generic;
using Game.Buildings;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab),
	typeof(NetPrefab),
	typeof(RoutePrefab)
})]
public class ServiceUpgrade : ComponentBase
{
	public BuildingPrefab[] m_Buildings;

	public uint m_UpgradeCost = 100u;

	public int m_XPReward;

	public int m_MaxPlacementOffset = -1;

	public float m_MaxPlacementDistance;

	public bool m_ForbidMultiple;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Buildings != null)
		{
			for (int i = 0; i < m_Buildings.Length; i++)
			{
				prefabs.Add(m_Buildings[i]);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ServiceUpgradeData>());
		components.Add(ComponentType.ReadWrite<ServiceUpgradeBuilding>());
		if (GetComponent<BuildingPrefab>() != null)
		{
			components.Add(ComponentType.ReadWrite<PlaceableObjectData>());
			components.Add(ComponentType.ReadWrite<PlaceableInfoviewItem>());
		}
		if (base.prefab.TryGet<ServiceConsumption>(out var component) && component.m_Upkeep > 0)
		{
			components.Add(ComponentType.ReadWrite<ServiceUpkeepData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.ServiceUpgrade>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new ServiceUpgradeData
		{
			m_UpgradeCost = m_UpgradeCost,
			m_XPReward = m_XPReward,
			m_MaxPlacementOffset = m_MaxPlacementOffset,
			m_MaxPlacementDistance = m_MaxPlacementDistance,
			m_ForbidMultiple = m_ForbidMultiple
		});
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_Buildings == null)
		{
			return;
		}
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Buildings.Length; i++)
		{
			BuildingPrefab buildingPrefab = m_Buildings[i];
			if (!(buildingPrefab == null))
			{
				entityManager.GetBuffer<ServiceUpgradeBuilding>(entity).Add(new ServiceUpgradeBuilding(existingSystemManaged.GetEntity(buildingPrefab)));
				buildingPrefab.AddUpgrade(entityManager, this);
			}
		}
	}
}
