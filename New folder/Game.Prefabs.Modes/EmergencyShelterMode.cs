using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class EmergencyShelterMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_ShelterCapacityMultiplier;

		public int m_VehicleCapacity;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			GroundWaterPowered component = m_ModeDatas[i].m_Prefab.GetComponent<GroundWaterPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<EmergencyShelterData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			GroundWaterPowered component = modeData.m_Prefab.GetComponent<GroundWaterPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			EmergencyShelterData componentData = entityManager.GetComponentData<EmergencyShelterData>(entity);
			componentData.m_ShelterCapacity = (int)((float)componentData.m_ShelterCapacity * modeData.m_ShelterCapacityMultiplier);
			componentData.m_VehicleCapacity = modeData.m_VehicleCapacity;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			EmergencyShelter component = m_ModeDatas[i].m_Prefab.GetComponent<EmergencyShelter>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			EmergencyShelterData componentData = entityManager.GetComponentData<EmergencyShelterData>(entity);
			componentData.m_ShelterCapacity = component.m_ShelterCapacity;
			componentData.m_VehicleCapacity = component.m_VehicleCapacity;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
