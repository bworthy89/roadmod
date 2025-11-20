using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class GroundWaterPoweredMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_ProductionMultiplier;

		public float m_MaximumGroundWaterMultiplier;
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
			entityManager.GetComponentData<GroundWaterPoweredData>(entity);
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
			GroundWaterPoweredData componentData = entityManager.GetComponentData<GroundWaterPoweredData>(entity);
			componentData.m_MaximumGroundWater = (int)((float)componentData.m_MaximumGroundWater * modeData.m_MaximumGroundWaterMultiplier);
			componentData.m_Production = (int)((float)componentData.m_Production * modeData.m_ProductionMultiplier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
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
			GroundWaterPoweredData componentData = entityManager.GetComponentData<GroundWaterPoweredData>(entity);
			componentData.m_MaximumGroundWater = component.m_MaximumGroundWater;
			componentData.m_Production = component.m_Production;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
