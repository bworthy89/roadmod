using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class WindPoweredMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_MaximumWindMultiplier;

		public float m_ProductionMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WindPowered component = m_ModeDatas[i].m_Prefab.GetComponent<WindPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<WindPoweredData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			WindPowered component = modeData.m_Prefab.GetComponent<WindPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WindPoweredData componentData = entityManager.GetComponentData<WindPoweredData>(entity);
			componentData.m_MaximumWind *= modeData.m_MaximumWindMultiplier;
			componentData.m_Production = (int)((float)componentData.m_Production * modeData.m_ProductionMultiplier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			WindPowered component = m_ModeDatas[i].m_Prefab.GetComponent<WindPowered>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			WindPoweredData componentData = entityManager.GetComponentData<WindPoweredData>(entity);
			componentData.m_MaximumWind = component.m_MaximumWind;
			componentData.m_Production = component.m_Production;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
