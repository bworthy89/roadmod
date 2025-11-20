using System;
using System.Collections.Generic;
using Game.Buildings;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[RequireComponent(typeof(PowerPlant))]
[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class WaterPowered : ComponentBase, IServiceUpgrade
{
	public float m_ProductionFactor;

	public float m_CapacityFactor;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WaterPoweredData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Game.Buildings.WaterPowered>());
			components.Add(ComponentType.ReadWrite<Efficiency>());
			components.Add(ComponentType.ReadWrite<RenewableElectricityProduction>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.WaterPowered>());
		components.Add(ComponentType.ReadWrite<Efficiency>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		if (base.prefab.TryGet<PowerPlant>(out var component) && component.m_ElectricityProduction != 0)
		{
			UnityEngine.Debug.LogErrorFormat(base.prefab, "WaterPowered has non-zero electricity production: {0}", base.prefab.name);
		}
		entityManager.SetComponentData(entity, new WaterPoweredData
		{
			m_ProductionFactor = m_ProductionFactor,
			m_CapacityFactor = m_CapacityFactor
		});
	}
}
