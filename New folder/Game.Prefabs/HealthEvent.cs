using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class HealthEvent : ComponentBase
{
	public EventTargetType m_RandomTargetType = EventTargetType.Citizen;

	public HealthEventType m_HealthEventType;

	public Bounds1 m_OccurenceProbability = new Bounds1(0f, 50f);

	public Bounds1 m_TransportProbability = new Bounds1(0f, 100f);

	public bool m_RequireTracking = true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<HealthEventData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Events.HealthEvent>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		HealthEventData componentData = default(HealthEventData);
		componentData.m_RandomTargetType = m_RandomTargetType;
		componentData.m_HealthEventType = m_HealthEventType;
		componentData.m_OccurenceProbability = m_OccurenceProbability;
		componentData.m_TransportProbability = m_TransportProbability;
		componentData.m_RequireTracking = m_RequireTracking;
		entityManager.SetComponentData(entity, componentData);
	}
}
