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
public class PoliceStation : ComponentBase, IServiceUpgrade
{
	public int m_PatrolCarCapacity = 10;

	public int m_PoliceHelicopterCapacity;

	public int m_JailCapacity = 15;

	[EnumFlag]
	public PolicePurpose m_Purposes = PolicePurpose.Patrol | PolicePurpose.Emergency;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PoliceStationData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.PoliceStation>());
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
			if (m_JailCapacity != 0)
			{
				components.Add(ComponentType.ReadWrite<Occupant>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceCoverage>() == null)
		{
			components.Add(ComponentType.ReadWrite<Game.Buildings.PoliceStation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			if (m_JailCapacity != 0)
			{
				components.Add(ComponentType.ReadWrite<Occupant>());
			}
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		PoliceStationData componentData = default(PoliceStationData);
		componentData.m_PatrolCarCapacity = m_PatrolCarCapacity;
		componentData.m_PoliceHelicopterCapacity = m_PoliceHelicopterCapacity;
		componentData.m_JailCapacity = m_JailCapacity;
		componentData.m_PurposeMask = m_Purposes;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(8));
	}
}
