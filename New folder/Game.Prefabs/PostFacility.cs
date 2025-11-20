using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Buildings;
using Game.Economy;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class PostFacility : ComponentBase, IServiceUpgrade
{
	public int m_PostVanCapacity = 10;

	public int m_PostTruckCapacity;

	public int m_MailStorageCapacity = 100000;

	public int m_MailBoxCapacity = 10000;

	public int m_SortingRate;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PostFacilityData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
		if (m_MailBoxCapacity > 0)
		{
			components.Add(ComponentType.ReadWrite<MailBoxData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.PostFacility>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Resources>());
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<GuestVehicle>());
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			if (m_PostTruckCapacity > 0)
			{
				components.Add(ComponentType.ReadWrite<ServiceDispatch>());
				components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			}
			if (m_PostVanCapacity > 0)
			{
				components.Add(ComponentType.ReadWrite<ServiceDispatch>());
				components.Add(ComponentType.ReadWrite<ServiceDistrict>());
				components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			}
			if (m_MailBoxCapacity > 0)
			{
				components.Add(ComponentType.ReadWrite<Game.Routes.MailBox>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.PostFacility>());
		components.Add(ComponentType.ReadWrite<Resources>());
		if (m_PostTruckCapacity > 0)
		{
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
		if (m_PostVanCapacity > 0)
		{
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
		if (m_MailBoxCapacity > 0)
		{
			components.Add(ComponentType.ReadWrite<Game.Routes.MailBox>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		PostFacilityData componentData = default(PostFacilityData);
		componentData.m_PostVanCapacity = m_PostVanCapacity;
		componentData.m_PostTruckCapacity = m_PostTruckCapacity;
		componentData.m_MailCapacity = m_MailStorageCapacity;
		componentData.m_SortingRate = m_SortingRate;
		entityManager.SetComponentData(entity, componentData);
		if (m_MailBoxCapacity > 0)
		{
			MailBoxData componentData2 = default(MailBoxData);
			componentData2.m_MailCapacity = m_MailBoxCapacity;
			entityManager.SetComponentData(entity, componentData2);
		}
		entityManager.SetComponentData(entity, new UpdateFrameData(11));
	}
}
