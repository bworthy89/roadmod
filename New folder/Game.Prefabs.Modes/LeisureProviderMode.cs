using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class LeisureProviderMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public BuildingPrefab m_Prefab;

		public float m_EfficiencyMultifier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			LeisureProvider component = m_ModeDatas[i].m_Prefab.GetComponent<LeisureProvider>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<LeisureProviderData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			LeisureProvider component = modeData.m_Prefab.GetComponent<LeisureProvider>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			LeisureProviderData componentData = entityManager.GetComponentData<LeisureProviderData>(entity);
			componentData.m_Efficiency = (int)((float)componentData.m_Efficiency * modeData.m_EfficiencyMultifier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			LeisureProvider component = m_ModeDatas[i].m_Prefab.GetComponent<LeisureProvider>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			LeisureProviderData componentData = entityManager.GetComponentData<LeisureProviderData>(entity);
			componentData.m_Efficiency = component.m_Efficiency;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
