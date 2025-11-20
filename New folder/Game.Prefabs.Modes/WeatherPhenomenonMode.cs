using System;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class WeatherPhenomenonMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public EventPrefab m_Prefab;

		public float m_OccurrenceProbability;

		public Bounds1 m_OccurenceTemperature;

		public Bounds1 m_OccurenceRain;

		public Bounds1 m_OccurenceCloudiness;

		public Bounds1 m_Duration;

		public Bounds1 m_PhenomenonRadius;

		public Bounds1 m_HotspotRadius;

		public float m_HotspotInstability;

		public float m_DamageSeverity;

		public float m_DangerLevel;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WeatherPhenomenon component = m_ModeDatas[i].m_Prefab.GetComponent<WeatherPhenomenon>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<WeatherPhenomenonData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			WeatherPhenomenon component = modeData.m_Prefab.GetComponent<WeatherPhenomenon>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WeatherPhenomenonData componentData = entityManager.GetComponentData<WeatherPhenomenonData>(entity);
			componentData.m_OccurenceProbability = modeData.m_OccurrenceProbability;
			componentData.m_OccurenceTemperature = modeData.m_OccurenceTemperature;
			componentData.m_OccurenceRain = modeData.m_OccurenceRain;
			componentData.m_OccurenceCloudiness = modeData.m_OccurenceCloudiness;
			componentData.m_Duration = modeData.m_Duration;
			componentData.m_PhenomenonRadius = modeData.m_PhenomenonRadius;
			componentData.m_HotspotRadius = modeData.m_HotspotRadius;
			componentData.m_HotspotInstability = modeData.m_HotspotInstability;
			componentData.m_DamageSeverity = modeData.m_DamageSeverity;
			componentData.m_DangerLevel = modeData.m_DangerLevel;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WeatherPhenomenon component = m_ModeDatas[i].m_Prefab.GetComponent<WeatherPhenomenon>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WeatherPhenomenonData componentData = entityManager.GetComponentData<WeatherPhenomenonData>(entity);
			componentData.m_OccurenceProbability = component.m_OccurrenceProbability;
			componentData.m_OccurenceTemperature = component.m_OccurenceTemperature;
			componentData.m_OccurenceRain = component.m_OccurenceRain;
			componentData.m_OccurenceCloudiness = component.m_OccurenceCloudiness;
			componentData.m_Duration = component.m_Duration;
			componentData.m_PhenomenonRadius = component.m_PhenomenonRadius;
			componentData.m_HotspotRadius = component.m_HotspotRadius;
			componentData.m_HotspotInstability = component.m_HotspotInstability;
			componentData.m_DamageSeverity = component.m_DamageSeverity;
			componentData.m_DangerLevel = component.m_DangerLevel;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
