using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class TrafficLightObject : ComponentBase
{
	public bool m_VehicleLeft;

	public bool m_VehicleRight;

	public bool m_CrossingLeft;

	public bool m_CrossingRight;

	public bool m_AllowFlipped;

	public Bounds1 m_ReachOffset;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TrafficLightData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TrafficLight>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		TrafficLightData componentData = default(TrafficLightData);
		componentData.m_Type = (TrafficLightType)0;
		componentData.m_ReachOffset = m_ReachOffset;
		if (m_VehicleLeft)
		{
			componentData.m_Type |= TrafficLightType.VehicleLeft;
		}
		if (m_VehicleRight)
		{
			componentData.m_Type |= TrafficLightType.VehicleRight;
		}
		if (m_CrossingLeft)
		{
			componentData.m_Type |= TrafficLightType.CrossingLeft;
		}
		if (m_CrossingRight)
		{
			componentData.m_Type |= TrafficLightType.CrossingRight;
		}
		if (m_AllowFlipped)
		{
			componentData.m_Type |= TrafficLightType.AllowFlipped;
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
