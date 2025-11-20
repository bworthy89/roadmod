using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Companies/", new Type[] { })]
public class BrandPrefab : PrefabBase
{
	public CompanyPrefab[] m_Companies;

	public Color[] m_BrandColors;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Companies.Length; i++)
		{
			prefabs.Add(m_Companies[i]);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<BrandData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		BrandData componentData = default(BrandData);
		for (int i = 0; i < m_BrandColors.Length; i++)
		{
			componentData.m_ColorSet[i] = m_BrandColors[i];
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
