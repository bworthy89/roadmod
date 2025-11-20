using System;
using System.Collections.Generic;
using Game.Objects;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { typeof(CarPrefab) })]
public class MaintenanceVehicle : ComponentBase
{
	public MaintenanceType m_MaintenanceType = MaintenanceType.Park;

	public int m_MaintenanceCapacity = 1000;

	public int m_MaintenanceRate = 200;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MaintenanceVehicleData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.MaintenanceVehicle>());
		if (components.Contains(ComponentType.ReadWrite<Moving>()))
		{
			components.Add(ComponentType.ReadWrite<PathInformation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		}
		if ((m_MaintenanceType & MaintenanceType.Park) != MaintenanceType.None)
		{
			components.Add(ComponentType.ReadWrite<ParkMaintenanceVehicle>());
		}
		if ((m_MaintenanceType & (MaintenanceType.Road | MaintenanceType.Snow | MaintenanceType.Vehicle)) != MaintenanceType.None)
		{
			components.Add(ComponentType.ReadWrite<RoadMaintenanceVehicle>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new MaintenanceVehicleData(m_MaintenanceType, m_MaintenanceCapacity, m_MaintenanceRate));
		entityManager.SetComponentData(entity, new UpdateFrameData(7));
	}
}
