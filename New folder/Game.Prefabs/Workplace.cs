using System;
using System.Collections.Generic;
using Game.Companies;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class Workplace : ComponentBase, IServiceUpgrade
{
	[Tooltip("The max amount of workers for City Service Buildings. ATTENTION: the other companies' max amount of workers changed dynamically, the max amount of workers of them depend on the m_MaxWorkersPerCell of each company prefab data")]
	public int m_Workplaces;

	[Tooltip("The minimum amount of workers of this workplace")]
	public int m_MinimumWorkersLimit;

	public WorkplaceComplexity m_Complexity;

	public float m_EveningShiftProbability;

	public float m_NightShiftProbability;

	[Tooltip("Offset to employee happiness")]
	public int m_WorkConditions;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WorkplaceData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null && m_Workplaces > 0)
		{
			components.Add(ComponentType.ReadWrite<WorkProvider>());
			components.Add(ComponentType.ReadWrite<Employee>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		if (m_Workplaces > 0)
		{
			components.Add(ComponentType.ReadWrite<WorkProvider>());
			components.Add(ComponentType.ReadWrite<Employee>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new WorkplaceData
		{
			m_MaxWorkers = m_Workplaces,
			m_MinimumWorkersLimit = m_MinimumWorkersLimit,
			m_Complexity = m_Complexity,
			m_EveningShiftProbability = m_EveningShiftProbability,
			m_NightShiftProbability = m_NightShiftProbability,
			m_WorkConditions = m_WorkConditions
		});
	}
}
