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
	typeof(BuildingExtensionPrefab)
})]
public class Prison : ComponentBase, IServiceUpgrade
{
	public int m_PrisonVanCapacity = 10;

	public int m_PrisonerCapacity = 500;

	public sbyte m_PrisonerWellbeing;

	public sbyte m_PrisonerHealth;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PrisonData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.Prison>());
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
			if (m_PrisonerCapacity != 0)
			{
				components.Add(ComponentType.ReadWrite<Occupant>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.Prison>());
		components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		if (m_PrisonerCapacity != 0)
		{
			components.Add(ComponentType.ReadWrite<Occupant>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		PrisonData componentData = default(PrisonData);
		componentData.m_PrisonVanCapacity = m_PrisonVanCapacity;
		componentData.m_PrisonerCapacity = m_PrisonerCapacity;
		componentData.m_PrisonerWellbeing = m_PrisonerWellbeing;
		componentData.m_PrisonerHealth = m_PrisonerHealth;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(3));
	}
}
