using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Companies;
using Game.Economy;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
[RequireComponent(typeof(StorageLimit))]
public class CargoTransportStation : ComponentBase, IServiceUpgrade
{
	public ResourceInEditor[] m_TradedResources;

	public int transports;

	public EnergyTypes m_CarRefuelTypes;

	public EnergyTypes m_TrainRefuelTypes;

	public EnergyTypes m_WatercraftRefuelTypes;

	public EnergyTypes m_AircraftRefuelTypes;

	public float m_LoadingFactor;

	public float m_WorkMultiplier;

	public int2 m_TransportInterval;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TransportStationData>());
		components.Add(ComponentType.ReadWrite<CargoTransportStationData>());
		components.Add(ComponentType.ReadWrite<StorageCompanyData>());
		components.Add(ComponentType.ReadWrite<TransportCompanyData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.TransportStation>());
		components.Add(ComponentType.ReadWrite<Game.Buildings.CargoTransportStation>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			components.Add(ComponentType.ReadWrite<Game.Companies.StorageCompany>());
			components.Add(ComponentType.ReadWrite<TradeCost>());
			components.Add(ComponentType.ReadWrite<StorageTransferRequest>());
			components.Add(ComponentType.ReadWrite<Game.Economy.Resources>());
			if (transports > 0)
			{
				components.Add(ComponentType.ReadWrite<TransportCompany>());
				components.Add(ComponentType.ReadWrite<OwnedVehicle>());
			}
		}
		if (m_WorkMultiplier > 0f)
		{
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.TransportStation>());
		components.Add(ComponentType.ReadWrite<Game.Buildings.CargoTransportStation>());
		components.Add(ComponentType.ReadWrite<Game.Companies.StorageCompany>());
		components.Add(ComponentType.ReadWrite<TradeCost>());
		components.Add(ComponentType.ReadWrite<StorageTransferRequest>());
		components.Add(ComponentType.ReadWrite<Game.Economy.Resources>());
		if (transports > 0)
		{
			components.Add(ComponentType.ReadWrite<TransportCompany>());
			components.Add(ComponentType.ReadWrite<OwnedVehicle>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		StorageCompanyData componentData = new StorageCompanyData
		{
			m_StoredResources = Resource.NoResource
		};
		if (m_TradedResources != null && m_TradedResources.Length != 0)
		{
			for (int i = 0; i < m_TradedResources.Length; i++)
			{
				componentData.m_StoredResources |= EconomyUtils.GetResource(m_TradedResources[i]);
				componentData.m_TransportInterval = m_TransportInterval;
			}
		}
		entityManager.SetComponentData(entity, componentData);
		if (transports > 0)
		{
			entityManager.SetComponentData(entity, new TransportCompanyData
			{
				m_MaxTransports = transports
			});
		}
		TransportStationData componentData2 = entityManager.GetComponentData<TransportStationData>(entity);
		componentData2.m_CarRefuelTypes |= m_CarRefuelTypes;
		componentData2.m_TrainRefuelTypes |= m_TrainRefuelTypes;
		componentData2.m_WatercraftRefuelTypes |= m_WatercraftRefuelTypes;
		componentData2.m_AircraftRefuelTypes |= m_AircraftRefuelTypes;
		componentData2.m_LoadingFactor = m_LoadingFactor;
		entityManager.SetComponentData(entity, componentData2);
		CargoTransportStationData componentData3 = default(CargoTransportStationData);
		componentData3.m_WorkMultiplier = m_WorkMultiplier;
		entityManager.SetComponentData(entity, componentData3);
		entityManager.SetComponentData(entity, new UpdateFrameData(0));
	}
}
