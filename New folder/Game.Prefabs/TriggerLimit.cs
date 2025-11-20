using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[] { typeof(TriggerPrefab) })]
public class TriggerLimit : ComponentBase
{
	public float m_IntervalSeconds;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TriggerLimitData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new TriggerLimitData
		{
			m_FrameInterval = (uint)Mathf.RoundToInt(m_IntervalSeconds * 60f)
		});
	}
}
