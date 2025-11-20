using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/", new Type[] { typeof(TriggerPrefab) })]
public class TutorialActivationEvent : ComponentBase
{
	public TutorialPrefab[] m_Tutorials;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TutorialActivationEventData>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Tutorials != null)
		{
			for (int i = 0; i < m_Tutorials.Length; i++)
			{
				prefabs.Add(m_Tutorials[i]);
			}
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		DynamicBuffer<TutorialActivationEventData> buffer = entityManager.GetBuffer<TutorialActivationEventData>(entity);
		if (m_Tutorials != null)
		{
			for (int i = 0; i < m_Tutorials.Length; i++)
			{
				buffer.Add(new TutorialActivationEventData
				{
					m_Tutorial = orCreateSystemManaged.GetEntity(m_Tutorials[i])
				});
			}
		}
	}
}
