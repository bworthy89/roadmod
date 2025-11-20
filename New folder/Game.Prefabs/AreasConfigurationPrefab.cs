using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.Mathematics;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class AreasConfigurationPrefab : PrefabBase
{
	[NotNull]
	public AreaPrefab m_DefaultDistrictPrefab;

	[Tooltip("Maximum slope that is considered buildable land, for display in the map selection screen.\nMin and below: Fully buildable, Max and above: Not buildable")]
	public Bounds1 m_BuildableLandMaxSlope = new Bounds1(0.1f, 0.3f);

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_DefaultDistrictPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AreasConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new AreasConfigurationData
		{
			m_DefaultDistrictPrefab = existingSystemManaged.GetEntity(m_DefaultDistrictPrefab),
			m_BuildableLandMaxSlope = m_BuildableLandMaxSlope
		});
	}
}
