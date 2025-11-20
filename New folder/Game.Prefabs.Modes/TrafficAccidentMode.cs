using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class TrafficAccidentMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public EventPrefab m_Prefab;

		public float m_OccurrenceProbability;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			TrafficAccident component = m_ModeDatas[i].m_Prefab.GetComponent<TrafficAccident>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<TrafficAccidentData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			TrafficAccident component = modeData.m_Prefab.GetComponent<TrafficAccident>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TrafficAccidentData componentData = entityManager.GetComponentData<TrafficAccidentData>(entity);
			componentData.m_OccurenceProbability = modeData.m_OccurrenceProbability;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			TrafficAccident component = m_ModeDatas[i].m_Prefab.GetComponent<TrafficAccident>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TrafficAccidentData componentData = entityManager.GetComponentData<TrafficAccidentData>(entity);
			componentData.m_OccurenceProbability = component.m_OccurrenceProbability;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
