using System;
using System.Collections.Generic;
using Game.Events;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class WaterLevelChangeComponent : ComponentBase
{
	public WaterLevelTargetType m_TargetType;

	public WaterLevelChangeType m_ChangeType;

	public float m_EscalationDelay = 1f;

	public bool m_Evacuate;

	public bool m_StayIndoors;

	[Tooltip("How dangerous the disaster is for the cims in the city. Determines how likely cims will leave shelter while the disaster is ongoing")]
	[Range(0f, 1f)]
	public float m_DangerLevel = 1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WaterLevelChangeData>());
		if (m_ChangeType == WaterLevelChangeType.RainControlled)
		{
			components.Add(ComponentType.ReadWrite<FloodData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WaterLevelChange>());
		components.Add(ComponentType.ReadWrite<Duration>());
		components.Add(ComponentType.ReadWrite<DangerLevel>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
		if (m_ChangeType == WaterLevelChangeType.RainControlled)
		{
			components.Add(ComponentType.ReadWrite<Flood>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		WaterLevelChangeData componentData = default(WaterLevelChangeData);
		componentData.m_TargetType = m_TargetType;
		componentData.m_ChangeType = m_ChangeType;
		componentData.m_EscalationDelay = m_EscalationDelay;
		componentData.m_DangerFlags = (DangerFlags)0u;
		if (m_Evacuate)
		{
			componentData.m_DangerFlags = DangerFlags.Evacuate;
		}
		if (m_StayIndoors)
		{
			componentData.m_DangerFlags = DangerFlags.StayIndoors;
		}
		componentData.m_DangerLevel = m_DangerLevel;
		entityManager.SetComponentData(entity, componentData);
	}
}
