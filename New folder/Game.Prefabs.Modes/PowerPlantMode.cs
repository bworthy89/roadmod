using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class PowerPlantMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_ElectricityProductionMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			PowerPlant component = m_ModeDatas[i].m_Prefab.GetComponent<PowerPlant>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<PowerPlantData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			PowerPlant component = modeData.m_Prefab.GetComponent<PowerPlant>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PowerPlantData componentData = entityManager.GetComponentData<PowerPlantData>(entity);
			componentData.m_ElectricityProduction = (int)((float)componentData.m_ElectricityProduction * modeData.m_ElectricityProductionMultiplier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			PowerPlant component = m_ModeDatas[i].m_Prefab.GetComponent<PowerPlant>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PowerPlantData componentData = entityManager.GetComponentData<PowerPlantData>(entity);
			componentData.m_ElectricityProduction = component.m_ElectricityProduction;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
