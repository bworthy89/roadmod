using System;
using System.Collections.Generic;
using Game.Buildings;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class PowerPlant : ComponentBase, IServiceUpgrade
{
	public int m_ElectricityProduction;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PowerPlantData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			components.Add(ComponentType.ReadWrite<ElectricityProducer>());
			components.Add(ComponentType.ReadWrite<ServiceUsage>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ElectricityProducer>());
		components.Add(ComponentType.ReadWrite<Efficiency>());
		components.Add(ComponentType.ReadWrite<ServiceUsage>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new PowerPlantData
		{
			m_ElectricityProduction = m_ElectricityProduction
		});
	}
}
