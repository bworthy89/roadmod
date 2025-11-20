using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetLanePrefab) })]
public class LaneDeterioration : ComponentBase
{
	public float m_TrafficDeterioration = 0.01f;

	public float m_TimeDeterioration = 0.5f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<LaneDeteriorationData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (!components.Contains(ComponentType.ReadWrite<MasterLane>()))
		{
			components.Add(ComponentType.ReadWrite<LaneCondition>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		LaneDeteriorationData componentData = default(LaneDeteriorationData);
		componentData.m_TrafficFactor = m_TrafficDeterioration;
		componentData.m_TimeFactor = m_TimeDeterioration;
		entityManager.SetComponentData(entity, componentData);
	}
}
