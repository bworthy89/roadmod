using System;
using System.Collections.Generic;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class TrafficAccident : ComponentBase
{
	public EventTargetType m_RandomSiteType = EventTargetType.Road;

	public EventTargetType m_SubjectType = EventTargetType.MovingCar;

	public TrafficAccidentType m_AccidentType;

	public float m_OccurrenceProbability = 0.01f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TrafficAccidentData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Events.TrafficAccident>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		TrafficAccidentData componentData = default(TrafficAccidentData);
		componentData.m_RandomSiteType = m_RandomSiteType;
		componentData.m_SubjectType = m_SubjectType;
		componentData.m_AccidentType = m_AccidentType;
		componentData.m_OccurenceProbability = m_OccurrenceProbability;
		entityManager.SetComponentData(entity, componentData);
	}
}
