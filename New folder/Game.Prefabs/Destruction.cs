using System;
using System.Collections.Generic;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class Destruction : ComponentBase
{
	public EventTargetType m_RandomTargetType;

	public float m_OccurenceProbability = 0.01f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<DestructionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Events.Destruction>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DestructionData componentData = default(DestructionData);
		componentData.m_RandomTargetType = m_RandomTargetType;
		componentData.m_OccurenceProbability = m_OccurenceProbability;
		entityManager.SetComponentData(entity, componentData);
	}
}
