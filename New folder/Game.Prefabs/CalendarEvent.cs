using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Events;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class CalendarEvent : ComponentBase
{
	public EventTargetType m_RandomTargetType = EventTargetType.Couple;

	public Bounds1 m_AffectedProbability = new Bounds1(25f, 25f);

	public Bounds1 m_OccurenceProbability = new Bounds1(100f, 100f);

	[EnumFlag]
	public CalendarEventMonths m_AllowedMonths;

	[EnumFlag]
	public CalendarEventTimes m_AllowedTimes;

	[Tooltip("In fourths of a day")]
	public int m_Duration;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CalendarEventData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Events.CalendarEvent>());
		components.Add(ComponentType.ReadWrite<Duration>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		CalendarEventData componentData = default(CalendarEventData);
		componentData.m_RandomTargetType = m_RandomTargetType;
		componentData.m_AffectedProbability = m_AffectedProbability;
		componentData.m_OccurenceProbability = m_OccurenceProbability;
		componentData.m_AllowedMonths = m_AllowedMonths;
		componentData.m_AllowedTimes = m_AllowedTimes;
		componentData.m_Duration = m_Duration;
		entityManager.SetComponentData(entity, componentData);
	}
}
