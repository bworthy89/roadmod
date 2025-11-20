using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
public class SpawnableBuilding : ComponentBase
{
	public ZonePrefab m_ZoneType;

	public ResourceStackInEditor[] m_LevelUpResources;

	[Range(1f, 5f)]
	public byte m_Level;

	public override bool ignoreUnlockDependencies => true;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			if (m_ZoneType != null && m_ZoneType.TryGet<UIObject>(out var component) && component.m_Group != null && component.m_Group is UIAssetCategoryPrefab uIAssetCategoryPrefab)
			{
				yield return "SpawnableBuilding" + uIAssetCategoryPrefab.name;
			}
		}
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_ZoneType);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SpawnableBuildingData>());
		components.Add(ComponentType.ReadWrite<BuildingSpawnGroupData>());
		if (m_ZoneType != null)
		{
			m_ZoneType.GetBuildingPrefabComponents(components, (BuildingPrefab)base.prefab, m_Level);
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BuildingCondition>());
		if (m_ZoneType != null)
		{
			if (m_ZoneType.Has<RandomLocalization>())
			{
				components.Add(ComponentType.ReadWrite<RandomLocalizationIndex>());
			}
			m_ZoneType.GetBuildingArchetypeComponents(components, (BuildingPrefab)base.prefab, m_Level);
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_ZoneType != null)
		{
			m_ZoneType.InitializeBuilding(entityManager, entity, (BuildingPrefab)base.prefab, m_Level);
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		if (m_LevelUpResources != null && m_LevelUpResources.Length != 0)
		{
			DynamicBuffer<LevelUpResourceData> dynamicBuffer = entityManager.AddBuffer<LevelUpResourceData>(entity);
			for (int i = 0; i < m_LevelUpResources.Length; i++)
			{
				dynamicBuffer.Add(new LevelUpResourceData
				{
					m_LevelUpResource = new ResourceStack
					{
						m_Resource = EconomyUtils.GetResource(m_LevelUpResources[i].m_Resource),
						m_Amount = m_LevelUpResources[i].m_Amount
					}
				});
			}
		}
		SpawnableBuildingData componentData = new SpawnableBuildingData
		{
			m_Level = m_Level
		};
		if (m_ZoneType != null)
		{
			componentData.m_ZonePrefab = existingSystemManaged.GetEntity(m_ZoneType);
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
