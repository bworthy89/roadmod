using System;
using System.Collections.Generic;
using Game.Citizens;
using Game.City;
using Game.Economy;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class CityServiceBuilding : ComponentBase, IServiceUpgrade
{
	public ServiceUpkeepItem[] m_Upkeeps;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		if ((m_Upkeeps != null && m_Upkeeps.Length != 0) || (base.prefab.TryGet<ServiceConsumption>(out var component) && component.m_Upkeep > 0))
		{
			components.Add(ComponentType.ReadWrite<ServiceUpkeepData>());
		}
		components.Add(ComponentType.ReadWrite<CollectedServiceBuildingBudgetData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<CityServiceUpkeep>());
			components.Add(ComponentType.ReadWrite<Resources>());
			components.Add(ComponentType.ReadWrite<TripNeeded>());
			components.Add(ComponentType.ReadWrite<GuestVehicle>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CityServiceUpkeep>());
		components.Add(ComponentType.ReadWrite<Resources>());
		components.Add(ComponentType.ReadWrite<TripNeeded>());
		components.Add(ComponentType.ReadWrite<GuestVehicle>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DynamicBuffer<ServiceUpkeepData> dynamicBuffer = entityManager.AddBuffer<ServiceUpkeepData>(entity);
		if (m_Upkeeps != null)
		{
			ServiceUpkeepItem[] upkeeps = m_Upkeeps;
			foreach (ServiceUpkeepItem serviceUpkeepItem in upkeeps)
			{
				dynamicBuffer.Add(new ServiceUpkeepData
				{
					m_Upkeep = new ResourceStack
					{
						m_Resource = EconomyUtils.GetResource(serviceUpkeepItem.m_Resources.m_Resource),
						m_Amount = serviceUpkeepItem.m_Resources.m_Amount
					},
					m_ScaleWithUsage = serviceUpkeepItem.m_ScaleWithUsage
				});
			}
		}
	}
}
