using System;
using System.Collections.Generic;
using Game.City;
using Game.Triggers;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[] { })]
public class StatisticTriggerPrefab : PrefabBase
{
	public StatisticTriggerType m_Type;

	public StatisticsPrefab m_StatisticPrefab;

	public int m_StatisticParameter;

	public StatisticsPrefab m_NormalizeWithPrefab;

	public int m_NormalizeWithParameter;

	public int m_TimeFrame = 1;

	public int m_MinSamples = 1;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_StatisticPrefab != null)
		{
			prefabs.Add(m_StatisticPrefab);
		}
		if (m_NormalizeWithPrefab != null)
		{
			prefabs.Add(m_NormalizeWithPrefab);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TriggerData>());
		components.Add(ComponentType.ReadWrite<StatisticTriggerData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		StatisticTriggerData componentData = new StatisticTriggerData
		{
			m_Type = m_Type
		};
		if (m_StatisticPrefab != null)
		{
			componentData.m_StatisticEntity = orCreateSystemManaged.GetEntity(m_StatisticPrefab);
		}
		componentData.m_StatisticParameter = m_StatisticParameter;
		if (m_NormalizeWithPrefab != null)
		{
			componentData.m_NormalizeWithPrefab = orCreateSystemManaged.GetEntity(m_NormalizeWithPrefab);
		}
		componentData.m_NormalizeWithParameter = m_NormalizeWithParameter;
		componentData.m_TimeFrame = m_TimeFrame;
		componentData.m_MinSamples = m_MinSamples;
		if ((m_StatisticPrefab != null && m_StatisticPrefab.m_CollectionType == StatisticCollectionType.Daily) || (m_NormalizeWithPrefab != null && m_NormalizeWithPrefab.m_CollectionType == StatisticCollectionType.Daily))
		{
			componentData.m_MinSamples = math.max(componentData.m_MinSamples, 32 + math.max(0, m_TimeFrame - 1));
		}
		entityManager.SetComponentData(entity, componentData);
		entityManager.GetBuffer<TriggerData>(entity).Add(new TriggerData
		{
			m_TriggerType = TriggerType.StatisticsValue,
			m_TargetTypes = TargetType.Nothing,
			m_TriggerPrefab = entity
		});
	}
}
