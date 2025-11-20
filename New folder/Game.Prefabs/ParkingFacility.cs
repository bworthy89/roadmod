using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class ParkingFacility : ComponentBase, IServiceUpgrade
{
	public RoadTypes m_RoadTypes = RoadTypes.Car;

	public float m_ComfortFactor = 0.5f;

	public int m_GarageMarkerCapacity;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ParkingFacilityData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.ParkingFacility>());
		if ((m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
		{
			components.Add(ComponentType.ReadWrite<CarParkingFacility>());
		}
		if ((m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
		{
			components.Add(ComponentType.ReadWrite<BicycleParkingFacility>());
		}
		if (GetComponent<ServiceUpgrade>() == null && GetComponent<CityServiceBuilding>() != null)
		{
			components.Add(ComponentType.ReadWrite<Efficiency>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.ParkingFacility>());
		components.Add(ComponentType.ReadWrite<Efficiency>());
		if ((m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
		{
			components.Add(ComponentType.ReadWrite<CarParkingFacility>());
		}
		if ((m_RoadTypes & RoadTypes.Bicycle) != RoadTypes.None)
		{
			components.Add(ComponentType.ReadWrite<BicycleParkingFacility>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new ParkingFacilityData
		{
			m_RoadTypes = m_RoadTypes,
			m_ComfortFactor = m_ComfortFactor,
			m_GarageMarkerCapacity = m_GarageMarkerCapacity
		});
		entityManager.SetComponentData(entity, new UpdateFrameData(12));
	}
}
