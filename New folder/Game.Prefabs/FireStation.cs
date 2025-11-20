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
public class FireStation : ComponentBase, IServiceUpgrade
{
	public int m_FireEngineCapacity = 3;

	public int m_FireHelicopterCapacity;

	public int m_DisasterResponseCapacity;

	public float m_VehicleEfficiency = 1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<FireStationData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.FireStation>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			if (GetComponent<UniqueObject>() == null)
			{
				components.Add(ComponentType.ReadWrite<ServiceDistrict>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceCoverage>() == null)
		{
			components.Add(ComponentType.ReadWrite<Game.Buildings.FireStation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new FireStationData
		{
			m_FireEngineCapacity = m_FireEngineCapacity,
			m_FireHelicopterCapacity = m_FireHelicopterCapacity,
			m_DisasterResponseCapacity = m_DisasterResponseCapacity,
			m_VehicleEfficiency = m_VehicleEfficiency
		});
		entityManager.SetComponentData(entity, new UpdateFrameData(7));
	}
}
