using System;
using System.Collections.Generic;
using Game.City;
using Game.Simulation;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Services/", new Type[] { })]
public class ServicePrefab : PrefabBase
{
	[SerializeField]
	private PlayerResource[] m_CityResources;

	[SerializeField]
	private CityService m_Service;

	[SerializeField]
	private bool m_BudgetAdjustable = true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ServiceData>());
		components.Add(ComponentType.ReadWrite<CollectedCityServiceBudgetData>());
		components.Add(ComponentType.ReadWrite<CollectedCityServiceUpkeepData>());
		if (m_CityResources != null && m_CityResources.Length != 0)
		{
			components.Add(ComponentType.ReadWrite<CollectedCityServiceFeeData>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_CityResources != null && m_CityResources.Length != 0)
		{
			DynamicBuffer<CollectedCityServiceFeeData> buffer = entityManager.GetBuffer<CollectedCityServiceFeeData>(entity);
			for (int i = 0; i < m_CityResources.Length; i++)
			{
				buffer.Add(new CollectedCityServiceFeeData
				{
					m_PlayerResource = (int)m_CityResources[i]
				});
			}
		}
		entityManager.SetComponentData(entity, new ServiceData
		{
			m_Service = m_Service,
			m_BudgetAdjustable = m_BudgetAdjustable
		});
	}
}
