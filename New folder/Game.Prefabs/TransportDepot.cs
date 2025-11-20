using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Buildings;
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
public class TransportDepot : ComponentBase, IServiceUpgrade
{
	public TransportType m_TransportType;

	public EnergyTypes m_EnergyTypes = EnergyTypes.Fuel;

	public SizeClass m_SizeClass = SizeClass.Undefined;

	public int m_VehicleCapacity = 10;

	public float m_ProductionDuration;

	public float m_MaintenanceDuration;

	public bool m_DispatchCenter;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TransportDepotData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.TransportDepot>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			if (m_TransportType == TransportType.Taxi)
			{
				components.Add(ComponentType.ReadWrite<ServiceDistrict>());
			}
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.TransportDepot>());
		components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		components.Add(ComponentType.ReadWrite<OwnedVehicle>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		TransportDepotData componentData = default(TransportDepotData);
		componentData.m_TransportType = m_TransportType;
		componentData.m_DispatchCenter = m_DispatchCenter;
		componentData.m_EnergyTypes = m_EnergyTypes;
		componentData.m_SizeClass = m_SizeClass;
		componentData.m_VehicleCapacity = m_VehicleCapacity;
		componentData.m_ProductionDuration = m_ProductionDuration;
		componentData.m_MaintenanceDuration = m_MaintenanceDuration;
		if (m_SizeClass == SizeClass.Undefined)
		{
			componentData.m_SizeClass = ((m_TransportType != TransportType.Taxi) ? SizeClass.Large : SizeClass.Small);
		}
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(2));
	}
}
