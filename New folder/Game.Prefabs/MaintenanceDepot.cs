using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class MaintenanceDepot : ComponentBase, IServiceUpgrade
{
	public MaintenanceType m_MaintenanceType = MaintenanceType.Park;

	public int m_VehicleCapacity = 10;

	public float m_VehicleEfficiency = 1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MaintenanceDepotData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.MaintenanceDepot>());
		if ((m_MaintenanceType & MaintenanceType.Park) != MaintenanceType.None)
		{
			components.Add(ComponentType.ReadWrite<ParkMaintenance>());
		}
		if ((m_MaintenanceType & (MaintenanceType.Road | MaintenanceType.Snow | MaintenanceType.Vehicle)) != MaintenanceType.None)
		{
			components.Add(ComponentType.ReadWrite<RoadMaintenance>());
		}
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.MaintenanceDepot>());
		components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		components.Add(ComponentType.ReadWrite<OwnedVehicle>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		MaintenanceDepotData componentData = default(MaintenanceDepotData);
		componentData.m_MaintenanceType = m_MaintenanceType;
		componentData.m_VehicleCapacity = m_VehicleCapacity;
		componentData.m_VehicleEfficiency = m_VehicleEfficiency;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(10));
	}
}
