using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class ServiceCoverageMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_Range = 1000f;

		public float m_Capacity = 3000f;

		public float m_Magnitude = 1f;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ServiceCoverage component = m_ModeDatas[i].m_Prefab.GetComponent<ServiceCoverage>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<CoverageData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			ServiceCoverage component = modeData.m_Prefab.GetComponent<ServiceCoverage>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			CoverageData componentData = entityManager.GetComponentData<CoverageData>(entity);
			componentData.m_Capacity = modeData.m_Capacity;
			componentData.m_Range = modeData.m_Range;
			componentData.m_Magnitude = modeData.m_Magnitude;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ServiceCoverage component = m_ModeDatas[i].m_Prefab.GetComponent<ServiceCoverage>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			CoverageData componentData = entityManager.GetComponentData<CoverageData>(entity);
			componentData.m_Range = component.m_Range;
			componentData.m_Capacity = component.m_Capacity;
			componentData.m_Magnitude = component.m_Magnitude;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
