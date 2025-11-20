using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class WaterPumpingStationMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_CapacityMultifier;

		public float m_Purification;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WaterPumpingStation component = m_ModeDatas[i].m_Prefab.GetComponent<WaterPumpingStation>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<WaterPumpingStationData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			WaterPumpingStation component = modeData.m_Prefab.GetComponent<WaterPumpingStation>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WaterPumpingStationData componentData = entityManager.GetComponentData<WaterPumpingStationData>(entity);
			componentData.m_Capacity = (int)((float)componentData.m_Capacity * modeData.m_CapacityMultifier);
			componentData.m_Purification = modeData.m_Purification;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WaterPumpingStation component = m_ModeDatas[i].m_Prefab.GetComponent<WaterPumpingStation>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WaterPumpingStationData componentData = entityManager.GetComponentData<WaterPumpingStationData>(entity);
			componentData.m_Capacity = component.m_Capacity;
			componentData.m_Purification = component.m_Purification;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
