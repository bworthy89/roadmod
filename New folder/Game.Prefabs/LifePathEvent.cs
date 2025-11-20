using System;
using System.Collections.Generic;
using Game.Common;
using Game.Triggers;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[] { typeof(TriggerPrefab) })]
public class LifePathEvent : ComponentBase
{
	public LifePathEventType m_EventType;

	public bool m_IsChirp;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<LifePathEventData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Triggers.LifePathEvent>());
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
		hashSet.Add(ComponentType.ReadWrite<Game.Triggers.Chirp>());
		hashSet.Add(ComponentType.ReadWrite<ChirpEntity>());
		hashSet.Add(ComponentType.ReadWrite<PrefabRef>());
		LifePathEventData componentData = default(LifePathEventData);
		componentData.m_ChirpArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		componentData.m_IsChirp = m_IsChirp;
		componentData.m_EventType = m_EventType;
		entityManager.SetComponentData(entity, componentData);
	}
}
