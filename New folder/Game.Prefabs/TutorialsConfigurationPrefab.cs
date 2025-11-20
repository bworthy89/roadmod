using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class TutorialsConfigurationPrefab : PrefabBase
{
	[NotNull]
	public TutorialListPrefab m_TutorialsIntroList;

	[NotNull]
	public FeaturePrefab m_MapTilesPrefab;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_TutorialsIntroList);
		prefabs.Add(m_MapTilesPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TutorialsConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		Entity entity2 = existingSystemManaged.GetEntity(m_TutorialsIntroList);
		Entity entity3 = existingSystemManaged.GetEntity(m_MapTilesPrefab);
		entityManager.SetComponentData(entity, new TutorialsConfigurationData
		{
			m_TutorialsIntroList = entity2,
			m_MapTilesFeature = entity3
		});
	}
}
