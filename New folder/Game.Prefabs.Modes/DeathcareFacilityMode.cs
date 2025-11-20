using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class DeathcareFacilityMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_StorageCapacityMultifier;

		public float m_ProcessingRateMultifier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			DeathcareFacility component = m_ModeDatas[i].m_Prefab.GetComponent<DeathcareFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<DeathcareFacilityData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			DeathcareFacility component = modeData.m_Prefab.GetComponent<DeathcareFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			DeathcareFacilityData componentData = entityManager.GetComponentData<DeathcareFacilityData>(entity);
			componentData.m_StorageCapacity = (int)((float)componentData.m_StorageCapacity * modeData.m_StorageCapacityMultifier);
			componentData.m_ProcessingRate *= modeData.m_ProcessingRateMultifier;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			DeathcareFacility component = m_ModeDatas[i].m_Prefab.GetComponent<DeathcareFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			DeathcareFacilityData componentData = entityManager.GetComponentData<DeathcareFacilityData>(entity);
			componentData.m_StorageCapacity = component.m_StorageCapacity;
			componentData.m_ProcessingRate = component.m_ProcessingRate;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
