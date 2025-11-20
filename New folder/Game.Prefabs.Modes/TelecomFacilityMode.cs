using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class TelecomFacilityMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_RangeMultiplier;

		public float m_NetworkCapacityMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			TelecomFacility component = m_ModeDatas[i].m_Prefab.GetComponent<TelecomFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<TelecomFacilityData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			TelecomFacility component = modeData.m_Prefab.GetComponent<TelecomFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TelecomFacilityData componentData = entityManager.GetComponentData<TelecomFacilityData>(entity);
			componentData.m_Range = (int)(componentData.m_Range * modeData.m_RangeMultiplier);
			componentData.m_NetworkCapacity = (int)(componentData.m_NetworkCapacity * modeData.m_NetworkCapacityMultiplier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			TelecomFacility component = m_ModeDatas[i].m_Prefab.GetComponent<TelecomFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TelecomFacilityData componentData = entityManager.GetComponentData<TelecomFacilityData>(entity);
			componentData.m_Range = component.m_Range;
			componentData.m_NetworkCapacity = component.m_NetworkCapacity;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
