using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Serialization;

namespace Game.Prefabs;

public abstract class TutorialPhasePrefab : PrefabBase
{
	[Flags]
	public enum ControlScheme
	{
		KeyboardAndMouse = 1,
		Gamepad = 2,
		All = 3
	}

	public string m_Image;

	public string m_OverrideImagePS;

	public string m_OverrideImageXBox;

	public string m_Icon;

	public bool m_TitleVisible = true;

	[FormerlySerializedAs("m_ShowDescription")]
	public bool m_DescriptionVisible = true;

	public bool m_CanDeactivate;

	public ControlScheme m_ControlScheme = ControlScheme.All;

	[CanBeNull]
	public TutorialTriggerPrefabBase m_Trigger;

	public float m_OverrideCompletionDelay = -1f;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Trigger != null)
		{
			prefabs.Add(m_Trigger);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TutorialPhaseData>());
		if (m_Trigger != null)
		{
			components.Add(ComponentType.ReadWrite<TutorialTrigger>());
		}
		if (m_CanDeactivate)
		{
			components.Add(ComponentType.ReadWrite<TutorialPhaseCanDeactivate>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		if (entityManager.HasComponent<TutorialTrigger>(entity))
		{
			entityManager.SetComponentData(entity, new TutorialTrigger
			{
				m_Trigger = existingSystemManaged.GetEntity(m_Trigger)
			});
		}
	}

	public virtual void GenerateTutorialLinks(EntityManager entityManager, NativeParallelHashSet<Entity> linkedPrefabs)
	{
		if (m_Trigger != null)
		{
			m_Trigger.GenerateTutorialLinks(entityManager, linkedPrefabs);
		}
	}
}
