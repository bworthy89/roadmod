using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Economy;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
[RequireComponent(typeof(CityServiceBuilding))]
public class ResourceProducer : ComponentBase, IServiceUpgrade
{
	public ResourceProductionInfo[] m_Resources;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ResourceProductionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Game.Economy.Resources>());
			components.Add(ComponentType.ReadWrite<Game.Buildings.ResourceProducer>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Economy.Resources>());
		components.Add(ComponentType.ReadWrite<Game.Buildings.ResourceProducer>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Resources != null)
		{
			DynamicBuffer<ResourceProductionData> buffer = entityManager.GetBuffer<ResourceProductionData>(entity);
			buffer.ResizeUninitialized(m_Resources.Length);
			for (int i = 0; i < m_Resources.Length; i++)
			{
				ResourceProductionInfo resourceProductionInfo = m_Resources[i];
				buffer[i] = new ResourceProductionData(EconomyUtils.GetResource(resourceProductionInfo.m_Resource), resourceProductionInfo.m_ProductionRate, resourceProductionInfo.m_StorageCapacity);
			}
		}
	}
}
