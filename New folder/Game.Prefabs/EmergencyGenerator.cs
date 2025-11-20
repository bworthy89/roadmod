#define UNITY_ASSERTIONS
using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Buildings;
using Unity.Assertions;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class EmergencyGenerator : ComponentBase, IServiceUpgrade
{
	public int m_ElectricityProduction;

	[Tooltip("The emergency generator is activated when the charge drops below Min. It it disabled again when the charge reaches Max.")]
	public Bounds1 m_ActivationThreshold = new Bounds1(0.05f, 0.1f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		Assert.IsNotNull(GetComponent<ServiceUpgrade>(), "Only battery building service upgrades can function as emergency generators");
		components.Add(ComponentType.ReadWrite<EmergencyGeneratorData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ServiceUsage>());
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.EmergencyGenerator>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new EmergencyGeneratorData
		{
			m_ElectricityProduction = m_ElectricityProduction,
			m_ActivationThreshold = m_ActivationThreshold
		});
	}
}
