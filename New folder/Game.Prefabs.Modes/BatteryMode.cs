using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class BatteryMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_PowerOutputMultiplier;

		public float m_CapacityMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Battery component = m_ModeDatas[i].m_Prefab.GetComponent<Battery>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<BatteryData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			Battery component = modeData.m_Prefab.GetComponent<Battery>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			BatteryData componentData = entityManager.GetComponentData<BatteryData>(entity);
			componentData.m_PowerOutput = (int)((float)componentData.m_PowerOutput * modeData.m_PowerOutputMultiplier);
			componentData.m_Capacity = (int)((float)componentData.m_Capacity * modeData.m_CapacityMultiplier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Battery component = m_ModeDatas[i].m_Prefab.GetComponent<Battery>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			BatteryData componentData = entityManager.GetComponentData<BatteryData>(entity);
			componentData.m_PowerOutput = component.m_PowerOutput;
			componentData.m_Capacity = component.m_Capacity;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
