using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Tutorials;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Activation/", new Type[]
{
	typeof(TutorialPrefab),
	typeof(TutorialListPrefab)
})]
public class TutorialAutoActivation : TutorialActivation
{
	[CanBeNull]
	public PrefabBase m_RequiredUnlock;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_RequiredUnlock != null)
		{
			prefabs.Add(m_RequiredUnlock);
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AutoActivationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		Entity requiredUnlock = Entity.Null;
		if (m_RequiredUnlock != null)
		{
			requiredUnlock = existingSystemManaged.GetEntity(m_RequiredUnlock);
		}
		entityManager.SetComponentData(entity, new AutoActivationData
		{
			m_RequiredUnlock = requiredUnlock
		});
	}
}
