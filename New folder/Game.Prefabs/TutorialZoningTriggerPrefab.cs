using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Triggers/", new Type[] { })]
public class TutorialZoningTriggerPrefab : TutorialTriggerPrefabBase
{
	[NotNull]
	public ZonePrefab[] m_Zones;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		ZonePrefab[] zones = m_Zones;
		foreach (ZonePrefab zonePrefab in zones)
		{
			if (zonePrefab != null)
			{
				prefabs.Add(zonePrefab);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ZoningTriggerData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<ZoningTriggerData> buffer = entityManager.GetBuffer<ZoningTriggerData>(entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		ZonePrefab[] zones = m_Zones;
		foreach (ZonePrefab zonePrefab in zones)
		{
			buffer.Add(new ZoningTriggerData(existingSystemManaged.GetEntity(zonePrefab)));
		}
	}

	protected override void GenerateBlinkTags()
	{
		base.GenerateBlinkTags();
		ZonePrefab[] zones = m_Zones;
		foreach (ZonePrefab zonePrefab in zones)
		{
			if (zonePrefab.TryGet<UIObject>(out var component) && component.m_Group is UIAssetCategoryPrefab uIAssetCategoryPrefab && uIAssetCategoryPrefab.m_Menu != null)
			{
				AddBlinkTagAtPosition(zonePrefab.uiTag, 0);
				AddBlinkTagAtPosition(uIAssetCategoryPrefab.uiTag, 1);
				AddBlinkTagAtPosition(uIAssetCategoryPrefab.m_Menu.uiTag, 2);
			}
		}
	}

	public override void GenerateTutorialLinks(EntityManager entityManager, NativeParallelHashSet<Entity> linkedPrefabs)
	{
		base.GenerateTutorialLinks(entityManager, linkedPrefabs);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Zones.Length; i++)
		{
			linkedPrefabs.Add(existingSystemManaged.GetEntity(m_Zones[i]));
		}
	}
}
