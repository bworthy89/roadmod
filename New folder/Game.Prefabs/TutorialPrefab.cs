using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.Entities;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/", new Type[] { })]
public class TutorialPrefab : PrefabBase
{
	[NotNull]
	public TutorialPhasePrefab[] m_Phases;

	public int m_Priority;

	public bool m_ReplaceActive;

	public bool m_Mandatory;

	public bool m_EditorTutorial;

	public bool m_FireTelemetry;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		TutorialPhasePrefab[] phases = m_Phases;
		foreach (TutorialPhasePrefab item in phases)
		{
			prefabs.Add(item);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TutorialData>());
		components.Add(ComponentType.ReadWrite<TutorialPhaseRef>());
		if (m_ReplaceActive)
		{
			components.Add(ComponentType.ReadWrite<ReplaceActiveData>());
		}
		if (m_FireTelemetry)
		{
			components.Add(ComponentType.ReadOnly<TutorialFireTelemetry>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new TutorialData(m_Priority));
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<TutorialPhaseRef> buffer = entityManager.GetBuffer<TutorialPhaseRef>(entity);
		NativeParallelHashSet<Entity> linkedPrefabs = new NativeParallelHashSet<Entity>(5, Allocator.TempJob);
		TutorialPhasePrefab[] phases = m_Phases;
		foreach (TutorialPhasePrefab tutorialPhasePrefab in phases)
		{
			Entity entity2 = existingSystemManaged.GetEntity(tutorialPhasePrefab);
			TutorialPhaseRef elem = new TutorialPhaseRef
			{
				m_Phase = entity2
			};
			buffer.Add(elem);
			tutorialPhasePrefab.GenerateTutorialLinks(entityManager, linkedPrefabs);
		}
		foreach (Entity item in linkedPrefabs)
		{
			if (!entityManager.TryGetBuffer(item, isReadOnly: false, out DynamicBuffer<TutorialLinkData> buffer2))
			{
				buffer2 = entityManager.AddBuffer<TutorialLinkData>(item);
			}
			buffer2.Add(new TutorialLinkData
			{
				m_Tutorial = entity
			});
		}
		if (m_EditorTutorial)
		{
			entityManager.AddComponent<EditorTutorial>(entity);
		}
		linkedPrefabs.Dispose();
	}
}
