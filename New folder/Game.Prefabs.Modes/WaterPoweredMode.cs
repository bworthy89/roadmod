using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class WaterPoweredMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_ProductionFactor;

		public float m_CapacityFactor;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WaterPowered component = m_ModeDatas[i].m_Prefab.GetComponent<WaterPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<WaterPoweredData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			WaterPowered component = modeData.m_Prefab.GetComponent<WaterPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WaterPoweredData componentData = entityManager.GetComponentData<WaterPoweredData>(entity);
			componentData.m_ProductionFactor = modeData.m_ProductionFactor;
			componentData.m_CapacityFactor = modeData.m_CapacityFactor;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WaterPowered component = m_ModeDatas[i].m_Prefab.GetComponent<WaterPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WaterPoweredData componentData = entityManager.GetComponentData<WaterPoweredData>(entity);
			componentData.m_ProductionFactor = component.m_ProductionFactor;
			componentData.m_CapacityFactor = component.m_CapacityFactor;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
