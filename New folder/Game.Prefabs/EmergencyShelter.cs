using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Buildings;
using Game.Economy;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class EmergencyShelter : ComponentBase, IServiceUpgrade
{
	public int m_ShelterCapacity = 100;

	public int m_VehicleCapacity;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EmergencyShelterData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.EmergencyShelter>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
				components.Add(ComponentType.ReadWrite<ServiceUsage>());
			}
			components.Add(ComponentType.ReadWrite<Occupant>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			components.Add(ComponentType.ReadWrite<Resources>());
			if (GetComponent<UniqueObject>() == null)
			{
				components.Add(ComponentType.ReadWrite<ServiceDistrict>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.EmergencyShelter>());
		components.Add(ComponentType.ReadWrite<Occupant>());
		components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		components.Add(ComponentType.ReadWrite<Resources>());
		components.Add(ComponentType.ReadWrite<ServiceUsage>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		EmergencyShelterData componentData = default(EmergencyShelterData);
		componentData.m_ShelterCapacity = m_ShelterCapacity;
		componentData.m_VehicleCapacity = m_VehicleCapacity;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(15));
	}
}
