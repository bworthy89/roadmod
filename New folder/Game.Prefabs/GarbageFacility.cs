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
	typeof(BuildingExtensionPrefab),
	typeof(MarkerObjectPrefab)
})]
public class GarbageFacility : ComponentBase, IServiceUpgrade
{
	public int m_GarbageCapacity = 100000;

	public int m_VehicleCapacity = 10;

	public int m_TransportCapacity;

	public int m_ProcessingSpeed;

	public bool m_IndustrialWasteOnly;

	public bool m_LongTermStorage;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<GarbageFacilityData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.GarbageFacility>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Resources>());
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<GuestVehicle>());
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			if (m_VehicleCapacity > 0 || m_TransportCapacity > 0)
			{
				components.Add(ComponentType.ReadWrite<ServiceDispatch>());
				components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			}
			if (GetComponent<UniqueObject>() == null)
			{
				components.Add(ComponentType.ReadWrite<ServiceDistrict>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.GarbageFacility>());
		components.Add(ComponentType.ReadWrite<Resources>());
		if (m_VehicleCapacity > 0 || m_TransportCapacity > 0)
		{
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		GarbageFacilityData componentData = default(GarbageFacilityData);
		componentData.m_GarbageCapacity = m_GarbageCapacity;
		componentData.m_VehicleCapacity = m_VehicleCapacity;
		componentData.m_TransportCapacity = m_TransportCapacity;
		componentData.m_ProcessingSpeed = m_ProcessingSpeed;
		componentData.m_IndustrialWasteOnly = m_IndustrialWasteOnly;
		componentData.m_LongTermStorage = m_LongTermStorage;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(5));
	}
}
