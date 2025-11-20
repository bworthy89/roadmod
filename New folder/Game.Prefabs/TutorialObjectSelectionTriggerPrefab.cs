using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Annotations;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Triggers/", new Type[] { })]
public class TutorialObjectSelectionTriggerPrefab : TutorialTriggerPrefabBase
{
	[Serializable]
	public class ObjectSelectionTriggerInfo
	{
		[NotNull]
		public PrefabBase m_Trigger;

		[CanBeNull]
		public TutorialPhasePrefab m_GoToPhase;
	}

	[NotNull]
	public ObjectSelectionTriggerInfo[] m_Triggers;

	public override bool phaseBranching => m_Triggers.Any((ObjectSelectionTriggerInfo t) => t.m_GoToPhase != null);

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Triggers.Length; i++)
		{
			prefabs.Add(m_Triggers[i].m_Trigger);
			if (m_Triggers[i].m_GoToPhase != null)
			{
				prefabs.Add(m_Triggers[i].m_GoToPhase);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ObjectSelectionTriggerData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<ObjectSelectionTriggerData> buffer = entityManager.GetBuffer<ObjectSelectionTriggerData>(entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Triggers.Length; i++)
		{
			ObjectSelectionTriggerInfo objectSelectionTriggerInfo = m_Triggers[i];
			Entity entity2 = existingSystemManaged.GetEntity(objectSelectionTriggerInfo.m_Trigger);
			Entity goToPhase = ((objectSelectionTriggerInfo.m_GoToPhase == null) ? Entity.Null : existingSystemManaged.GetEntity(objectSelectionTriggerInfo.m_GoToPhase));
			buffer.Add(new ObjectSelectionTriggerData
			{
				m_Prefab = entity2,
				m_GoToPhase = goToPhase
			});
		}
		if (m_Triggers.Length <= 1)
		{
			return;
		}
		for (int j = 0; j < m_Triggers.Length; j++)
		{
			TutorialPhasePrefab goToPhase2 = m_Triggers[j].m_GoToPhase;
			if (goToPhase2 != null)
			{
				entityManager.AddComponent<TutorialPhaseBranch>(existingSystemManaged.GetEntity(goToPhase2));
			}
		}
	}

	public override void GenerateTutorialLinks(EntityManager entityManager, NativeParallelHashSet<Entity> linkedPrefabs)
	{
		base.GenerateTutorialLinks(entityManager, linkedPrefabs);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Triggers.Length; i++)
		{
			ObjectSelectionTriggerInfo objectSelectionTriggerInfo = m_Triggers[i];
			linkedPrefabs.Add(existingSystemManaged.GetEntity(objectSelectionTriggerInfo.m_Trigger));
		}
	}
}
