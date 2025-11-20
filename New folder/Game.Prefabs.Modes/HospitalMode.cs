using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class HospitalMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_PatientCapacityMultiplier;

		public int m_TreatmentBonus;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Hospital component = m_ModeDatas[i].m_Prefab.GetComponent<Hospital>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<HospitalData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			Hospital component = modeData.m_Prefab.GetComponent<Hospital>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			HospitalData componentData = entityManager.GetComponentData<HospitalData>(entity);
			componentData.m_PatientCapacity = (int)((float)componentData.m_PatientCapacity * modeData.m_PatientCapacityMultiplier);
			componentData.m_TreatmentBonus = modeData.m_TreatmentBonus;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Hospital component = m_ModeDatas[i].m_Prefab.GetComponent<Hospital>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			HospitalData componentData = entityManager.GetComponentData<HospitalData>(entity);
			componentData.m_PatientCapacity = component.m_PatientCapacity;
			componentData.m_TreatmentBonus = component.m_TreatmentBonus;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
