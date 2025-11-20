using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class CargoTransportStationMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_LoadingFactor;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			CargoTransportStation component = m_ModeDatas[i].m_Prefab.GetComponent<CargoTransportStation>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<TransportStationData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			CargoTransportStation component = modeData.m_Prefab.GetComponent<CargoTransportStation>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TransportStationData componentData = entityManager.GetComponentData<TransportStationData>(entity);
			componentData.m_LoadingFactor = modeData.m_LoadingFactor;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			CargoTransportStation component = m_ModeDatas[i].m_Prefab.GetComponent<CargoTransportStation>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			TransportStationData componentData = entityManager.GetComponentData<TransportStationData>(entity);
			componentData.m_LoadingFactor = component.m_LoadingFactor;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
