using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class PollutionMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		[Min(0f)]
		public float m_GroundPollutionMultiplier;

		[Min(0f)]
		public float m_AirPollutionMultiplier;

		[Min(0f)]
		public float m_NoisePollutionMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Pollution component = m_ModeDatas[i].m_Prefab.GetComponent<Pollution>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<PollutionData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			Pollution component = modeData.m_Prefab.GetComponent<Pollution>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PollutionData componentData = entityManager.GetComponentData<PollutionData>(entity);
			componentData.m_GroundPollution *= modeData.m_GroundPollutionMultiplier;
			componentData.m_AirPollution *= modeData.m_AirPollutionMultiplier;
			componentData.m_NoisePollution *= modeData.m_NoisePollutionMultiplier;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Pollution component = m_ModeDatas[i].m_Prefab.GetComponent<Pollution>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PollutionData componentData = entityManager.GetComponentData<PollutionData>(entity);
			componentData.m_GroundPollution = component.m_GroundPollution;
			componentData.m_AirPollution = component.m_AirPollution;
			componentData.m_NoisePollution = component.m_NoisePollution;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
