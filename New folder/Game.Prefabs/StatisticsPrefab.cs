using System;
using System.Collections.Generic;
using Game.City;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { })]
public class StatisticsPrefab : ArchetypePrefab
{
	public UIStatisticsCategoryPrefab m_Category;

	public UIStatisticsGroupPrefab m_Group;

	public StatisticType m_StatisticsType;

	public StatisticCollectionType m_CollectionType;

	public StatisticUnitType m_UnitType;

	public Color m_Color = Color.grey;

	public bool m_Stacked = true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<StatisticsData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<CityStatistic>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		Entity category = ((m_Category != null) ? existingSystemManaged.GetEntity(m_Category) : Entity.Null);
		Entity entity2 = ((m_Group != null) ? existingSystemManaged.GetEntity(m_Group) : Entity.Null);
		StatisticsData componentData = new StatisticsData
		{
			m_Group = entity2,
			m_Category = category,
			m_CollectionType = m_CollectionType,
			m_StatisticType = m_StatisticsType,
			m_UnitType = m_UnitType,
			m_Color = m_Color,
			m_Stacked = m_Stacked
		};
		entityManager.SetComponentData(entity, componentData);
	}

	public static Entity CreateInstance(EntityManager entityManager, Entity entity, ArchetypeData archetypeData, int parameter = 0)
	{
		Entity entity2 = entityManager.CreateEntity(archetypeData.m_Archetype);
		PrefabRef componentData = new PrefabRef
		{
			m_Prefab = entity
		};
		entityManager.AddComponentData(entity2, componentData);
		if (entityManager.HasComponent<StatisticParameter>(entity2))
		{
			entityManager.SetComponentData(entity2, new StatisticParameter
			{
				m_Value = parameter
			});
		}
		return entity2;
	}
}
