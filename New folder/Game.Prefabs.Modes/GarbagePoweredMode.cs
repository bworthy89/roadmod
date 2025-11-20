using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class GarbagePoweredMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_ProductionPerUnitMultiplier;

		public float m_CapacityMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			GarbagePowered component = m_ModeDatas[i].m_Prefab.GetComponent<GarbagePowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<GarbagePoweredData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			GarbagePowered component = modeData.m_Prefab.GetComponent<GarbagePowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			GarbagePoweredData componentData = entityManager.GetComponentData<GarbagePoweredData>(entity);
			componentData.m_ProductionPerUnit = (int)(componentData.m_ProductionPerUnit * modeData.m_ProductionPerUnitMultiplier);
			componentData.m_Capacity = (int)((float)componentData.m_Capacity * modeData.m_CapacityMultiplier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			GarbagePowered component = m_ModeDatas[i].m_Prefab.GetComponent<GarbagePowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			GarbagePoweredData componentData = entityManager.GetComponentData<GarbagePoweredData>(entity);
			componentData.m_ProductionPerUnit = component.m_ProductionPerUnit;
			componentData.m_Capacity = component.m_Capacity;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
