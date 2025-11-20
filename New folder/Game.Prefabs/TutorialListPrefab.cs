using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/", new Type[] { })]
public class TutorialListPrefab : PrefabBase
{
	public int m_Priority;

	[NotNull]
	public TutorialPrefab[] m_Tutorials;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		TutorialPrefab[] tutorials = m_Tutorials;
		foreach (TutorialPrefab item in tutorials)
		{
			prefabs.Add(item);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TutorialListData>());
		components.Add(ComponentType.ReadWrite<TutorialRef>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new TutorialListData(m_Priority));
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<TutorialRef> buffer = entityManager.GetBuffer<TutorialRef>(entity);
		TutorialPrefab[] tutorials = m_Tutorials;
		foreach (TutorialPrefab tutorialPrefab in tutorials)
		{
			Entity entity2 = existingSystemManaged.GetEntity(tutorialPrefab);
			TutorialRef elem = new TutorialRef
			{
				m_Tutorial = entity2
			};
			buffer.Add(elem);
		}
	}
}
