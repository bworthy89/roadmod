using System;
using System.Collections.Generic;
using Game.Economy;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[]
{
	typeof(CarPrefab),
	typeof(CarTrailerPrefab)
})]
public class DeliveryTruck : ComponentBase
{
	public int m_CargoCapacity = 10000;

	public int m_CostToDrive = 16;

	public ResourceInEditor[] m_TransportedResources;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<DeliveryTruckData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.DeliveryTruck>());
		if (base.prefab is CarPrefab)
		{
			components.Add(ComponentType.ReadWrite<PathInformation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		DeliveryTruckData componentData = new DeliveryTruckData
		{
			m_CargoCapacity = m_CargoCapacity,
			m_CostToDrive = m_CostToDrive
		};
		if (m_TransportedResources != null)
		{
			for (int i = 0; i < m_TransportedResources.Length; i++)
			{
				componentData.m_TransportedResources |= EconomyUtils.GetResource(m_TransportedResources[i]);
			}
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
