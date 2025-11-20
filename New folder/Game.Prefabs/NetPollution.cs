using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPrefab) })]
public class NetPollution : ComponentBase
{
	public float m_NoisePollutionFactor = 1f;

	public float m_AirPollutionFactor = 1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetPollutionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (components.Contains(ComponentType.ReadWrite<Node>()) || components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Net.Pollution>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		NetPollutionData componentData = default(NetPollutionData);
		componentData.m_Factors = new float2(m_NoisePollutionFactor, m_AirPollutionFactor);
		entityManager.SetComponentData(entity, componentData);
	}
}
