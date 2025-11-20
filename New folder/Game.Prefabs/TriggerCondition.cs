using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[]
{
	typeof(TriggerPrefab),
	typeof(StatisticTriggerPrefab)
})]
public class TriggerCondition : ComponentBase
{
	[SerializeField]
	public TriggerConditionData[] m_Conditions;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		if (m_Conditions != null && m_Conditions.Length != 0)
		{
			components.Add(ComponentType.ReadWrite<TriggerConditionData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_Conditions != null && m_Conditions.Length != 0)
		{
			DynamicBuffer<TriggerConditionData> buffer = entityManager.GetBuffer<TriggerConditionData>(entity);
			for (int i = 0; i < m_Conditions.Length; i++)
			{
				buffer.Add(m_Conditions[i]);
			}
		}
	}
}
