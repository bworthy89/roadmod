using System;
using System.Collections.Generic;
using Game.Audio.Radio;
using Game.Common;
using Game.Triggers;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[]
{
	typeof(TriggerPrefab),
	typeof(StatisticTriggerPrefab)
})]
public class RadioEvent : ComponentBase
{
	public Radio.SegmentType m_SegmentType = Radio.SegmentType.News;

	[Tooltip("Only for emergency events")]
	[ShowIf("m_SegmentType", 6, false)]
	public int m_EmergencyFrameDelay;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<RadioEventData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Triggers.RadioEvent>());
		components.Add(ComponentType.ReadWrite<PrefabRef>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		RadioEventData componentData = default(RadioEventData);
		componentData.m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		componentData.m_SegmentType = m_SegmentType;
		componentData.m_EmergencyFrameDelay = m_EmergencyFrameDelay;
		entityManager.SetComponentData(entity, componentData);
	}
}
