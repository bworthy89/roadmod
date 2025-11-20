using System;
using System.Collections.Generic;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class SpectatorEvent : ComponentBase
{
	public EventTargetType m_RandomSiteType = EventTargetType.TransportDepot;

	public float m_PreparationDuration = 0.1f;

	public float m_ActiveDuration = 0.1f;

	public float m_TerminationDuration = 0.1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SpectatorEventData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Events.SpectatorEvent>());
		components.Add(ComponentType.ReadWrite<Duration>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		SpectatorEventData componentData = default(SpectatorEventData);
		componentData.m_RandomSiteType = m_RandomSiteType;
		componentData.m_PreparationDuration = m_PreparationDuration;
		componentData.m_ActiveDuration = m_ActiveDuration;
		componentData.m_TerminationDuration = m_TerminationDuration;
		entityManager.SetComponentData(entity, componentData);
	}
}
