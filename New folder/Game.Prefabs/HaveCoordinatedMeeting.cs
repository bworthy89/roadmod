using System;
using System.Collections.Generic;
using Game.Citizens;
using Game.Economy;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class HaveCoordinatedMeeting : ComponentBase
{
	public CoordinatedMeetingPhase[] m_Phases;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<HaveCoordinatedMeetingData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CoordinatedMeeting>());
		components.Add(ComponentType.ReadWrite<CoordinatedMeetingAttendee>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
		components.Add(ComponentType.ReadWrite<PrefabRef>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Phases.Length; i++)
		{
			if (m_Phases[i].m_Notification != null)
			{
				prefabs.Add(m_Phases[i].m_Notification);
			}
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<HaveCoordinatedMeetingData> buffer = entityManager.GetBuffer<HaveCoordinatedMeetingData>(entity);
		if (m_Phases != null)
		{
			HaveCoordinatedMeetingData elem = default(HaveCoordinatedMeetingData);
			for (int i = 0; i < m_Phases.Length; i++)
			{
				CoordinatedMeetingPhase coordinatedMeetingPhase = m_Phases[i];
				TravelPurpose travelPurpose = new TravelPurpose
				{
					m_Purpose = coordinatedMeetingPhase.m_Purpose.m_Purpose,
					m_Data = coordinatedMeetingPhase.m_Purpose.m_Data,
					m_Resource = EconomyUtils.GetResource(coordinatedMeetingPhase.m_Purpose.m_Resource)
				};
				elem.m_TravelPurpose = travelPurpose;
				elem.m_Delay = coordinatedMeetingPhase.m_Delay;
				elem.m_Notification = ((coordinatedMeetingPhase.m_Notification != null) ? World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>().GetEntity(coordinatedMeetingPhase.m_Notification) : Entity.Null);
				buffer.Add(elem);
			}
		}
	}
}
