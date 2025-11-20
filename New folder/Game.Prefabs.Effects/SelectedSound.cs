using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs.Effects;

[ComponentMenu("Effects/", new Type[] { })]
public class SelectedSound : ComponentBase
{
	public EffectPrefab m_SelectedSound;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SelectedSoundData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		SelectedSoundData componentData = new SelectedSoundData
		{
			m_selectedSound = orCreateSystemManaged.GetEntity(m_SelectedSound)
		};
		entityManager.SetComponentData(entity, componentData);
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_SelectedSound);
	}
}
