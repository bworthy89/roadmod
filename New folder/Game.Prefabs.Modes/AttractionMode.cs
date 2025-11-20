using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class AttractionMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public BuildingPrefab m_Prefab;

		public float m_AttractivenessMultifier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Attraction component = m_ModeDatas[i].m_Prefab.GetComponent<Attraction>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<AttractionData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			Attraction component = modeData.m_Prefab.GetComponent<Attraction>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			AttractionData componentData = entityManager.GetComponentData<AttractionData>(entity);
			componentData.m_Attractiveness *= (int)((float)componentData.m_Attractiveness * modeData.m_AttractivenessMultifier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Attraction component = m_ModeDatas[i].m_Prefab.GetComponent<Attraction>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			AttractionData componentData = entityManager.GetComponentData<AttractionData>(entity);
			componentData.m_Attractiveness = component.m_Attractiveness;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
