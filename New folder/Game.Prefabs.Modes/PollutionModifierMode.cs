using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class PollutionModifierMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		[Range(-1f, 1f)]
		public float m_GroundPollutionMultiplier;

		[Range(-1f, 1f)]
		public float m_AirPollutionMultiplier;

		[Range(-1f, 1f)]
		public float m_NoisePollutionMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			PollutionModifier component = m_ModeDatas[i].m_Prefab.GetComponent<PollutionModifier>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<PollutionModifierData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			PollutionModifier component = modeData.m_Prefab.GetComponent<PollutionModifier>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PollutionModifierData componentData = entityManager.GetComponentData<PollutionModifierData>(entity);
			componentData.m_GroundPollutionMultiplier = modeData.m_GroundPollutionMultiplier;
			componentData.m_AirPollutionMultiplier = modeData.m_AirPollutionMultiplier;
			componentData.m_NoisePollutionMultiplier = modeData.m_NoisePollutionMultiplier;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			PollutionModifier component = m_ModeDatas[i].m_Prefab.GetComponent<PollutionModifier>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PollutionModifierData componentData = entityManager.GetComponentData<PollutionModifierData>(entity);
			componentData.m_GroundPollutionMultiplier = component.m_GroundPollutionMultiplier;
			componentData.m_AirPollutionMultiplier = component.m_AirPollutionMultiplier;
			componentData.m_NoisePollutionMultiplier = component.m_NoisePollutionMultiplier;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
