using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class FireMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public EventPrefab m_Prefab;

		public float m_StartProbability;

		public float m_StartIntensity;

		public float m_SpreadProbability;

		public float m_SpreadRange;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Fire component = m_ModeDatas[i].m_Prefab.GetComponent<Fire>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<FireData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			Fire component = modeData.m_Prefab.GetComponent<Fire>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			FireData componentData = entityManager.GetComponentData<FireData>(entity);
			componentData.m_StartProbability = modeData.m_StartProbability;
			componentData.m_StartIntensity = modeData.m_StartIntensity;
			componentData.m_SpreadProbability = modeData.m_SpreadProbability;
			componentData.m_SpreadRange = modeData.m_SpreadRange;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Fire component = m_ModeDatas[i].m_Prefab.GetComponent<Fire>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			FireData componentData = entityManager.GetComponentData<FireData>(entity);
			componentData.m_StartProbability = component.m_StartProbability;
			componentData.m_StartIntensity = component.m_StartIntensity;
			componentData.m_SpreadProbability = component.m_SpreadProbability;
			componentData.m_SpreadRange = component.m_SpreadRange;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
