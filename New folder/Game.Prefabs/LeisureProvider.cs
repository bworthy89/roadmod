using System;
using System.Collections.Generic;
using Game.Agents;
using Game.Buildings;
using Game.Economy;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab),
	typeof(CompanyPrefab)
})]
public class LeisureProvider : ComponentBase
{
	public int m_Efficiency;

	[HideInInspector]
	public ResourceInEditor m_Resources;

	public LeisureType m_LeisureType;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<LeisureProviderData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (m_Efficiency > 0)
		{
			components.Add(ComponentType.ReadWrite<Game.Buildings.LeisureProvider>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new LeisureProviderData
		{
			m_Efficiency = m_Efficiency,
			m_Resources = EconomyUtils.GetResource(m_Resources),
			m_LeisureType = m_LeisureType
		});
	}
}
