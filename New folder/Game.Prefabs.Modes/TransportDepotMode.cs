using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class TransportDepotMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public int m_VehicleCapacity = 10;

		public float m_ProductionDuration;

		public float m_MaintenanceDuration;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			TransportDepot component = m_ModeDatas[i].m_Prefab.GetComponent<TransportDepot>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<TransportDepotData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			TransportDepot component = modeData.m_Prefab.GetComponent<TransportDepot>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TransportDepotData componentData = entityManager.GetComponentData<TransportDepotData>(entity);
			componentData.m_VehicleCapacity = modeData.m_VehicleCapacity;
			componentData.m_ProductionDuration = modeData.m_ProductionDuration;
			componentData.m_MaintenanceDuration = modeData.m_MaintenanceDuration;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			TransportDepot component = m_ModeDatas[i].m_Prefab.GetComponent<TransportDepot>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TransportDepotData componentData = entityManager.GetComponentData<TransportDepotData>(entity);
			componentData.m_VehicleCapacity = component.m_VehicleCapacity;
			componentData.m_ProductionDuration = component.m_ProductionDuration;
			componentData.m_MaintenanceDuration = component.m_MaintenanceDuration;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
