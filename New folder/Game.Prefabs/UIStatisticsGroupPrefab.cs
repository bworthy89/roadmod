using System;
using System.Collections.Generic;
using Game.City;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public class UIStatisticsGroupPrefab : UIGroupPrefab
{
	public Color m_Color = Color.black;

	public UIStatisticsCategoryPrefab m_Category;

	public StatisticUnitType m_UnitType;

	public bool m_Stacked;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<UIStatisticsGroupData>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Category != null)
		{
			prefabs.Add(m_Category);
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		Entity category = ((m_Category != null) ? existingSystemManaged.GetEntity(m_Category) : Entity.Null);
		entityManager.SetComponentData(entity, new UIStatisticsGroupData
		{
			m_Category = category,
			m_Color = m_Color,
			m_UnitType = m_UnitType,
			m_Stacked = m_Stacked
		});
	}
}
