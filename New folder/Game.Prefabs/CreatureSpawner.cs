using System;
using System.Collections.Generic;
using Game.Creatures;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(ObjectPrefab) })]
public class CreatureSpawner : ComponentBase
{
	public int m_MaxGroupCount = 3;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceholderObjectElement>());
		components.Add(ComponentType.ReadWrite<CreatureSpawnData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Creatures.CreatureSpawner>());
		components.Add(ComponentType.ReadWrite<OwnedCreature>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		CreatureSpawnData componentData = default(CreatureSpawnData);
		componentData.m_MaxGroupCount = m_MaxGroupCount;
		entityManager.SetComponentData(entity, componentData);
	}
}
