using System;
using System.Collections.Generic;
using Game.Economy;
using Game.Objects;
using Game.Pathfind;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { typeof(VehiclePrefab) })]
public class CargoTransport : ComponentBase
{
	public int m_CargoCapacity = 10000;

	public int m_MaxResourceCount = 1;

	public float m_MaintenanceRange;

	public ResourceInEditor[] m_TransportedResources;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CargoTransportVehicleData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.CargoTransport>());
		components.Add(ComponentType.ReadWrite<Resources>());
		components.Add(ComponentType.ReadWrite<LoadingResources>());
		components.Add(ComponentType.ReadWrite<Odometer>());
		if (components.Contains(ComponentType.ReadWrite<Moving>()) && (!components.Contains(ComponentType.ReadWrite<Controller>()) || components.Contains(ComponentType.ReadWrite<LayoutElement>())))
		{
			components.Add(ComponentType.ReadWrite<PathInformation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		Resource resource = Resource.NoResource;
		if (m_TransportedResources != null)
		{
			for (int i = 0; i < m_TransportedResources.Length; i++)
			{
				resource |= EconomyUtils.GetResource(m_TransportedResources[i]);
			}
		}
		entityManager.SetComponentData(entity, new CargoTransportVehicleData(resource, m_CargoCapacity, m_MaxResourceCount, m_MaintenanceRange * 1000f));
	}
}
