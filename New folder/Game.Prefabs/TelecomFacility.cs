using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Common;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class TelecomFacility : ComponentBase, IServiceUpgrade
{
	public float m_Range = 1000f;

	public float m_NetworkCapacity = 10000f;

	public bool m_PenetrateTerrain;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TelecomFacilityData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.TelecomFacility>());
		components.Add(ComponentType.ReadWrite<PointOfInterest>());
		if (GetComponent<ServiceUpgrade>() == null && GetComponent<CityServiceBuilding>() != null)
		{
			components.Add(ComponentType.ReadWrite<Efficiency>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.TelecomFacility>());
		components.Add(ComponentType.ReadWrite<PointOfInterest>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new TelecomFacilityData
		{
			m_Range = m_Range,
			m_NetworkCapacity = m_NetworkCapacity,
			m_PenetrateTerrain = m_PenetrateTerrain
		});
		entityManager.SetComponentData(entity, new UpdateFrameData(13));
	}
}
