using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class PrisonMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_PrisonVanCapacityMultiplier;

		public sbyte m_PrisonerWellbeing;

		public sbyte m_PrisonerHealth;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Prison component = m_ModeDatas[i].m_Prefab.GetComponent<Prison>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<PrisonData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			Prison component = modeData.m_Prefab.GetComponent<Prison>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PrisonData componentData = entityManager.GetComponentData<PrisonData>(entity);
			componentData.m_PrisonVanCapacity = (int)((float)componentData.m_PrisonVanCapacity * modeData.m_PrisonVanCapacityMultiplier);
			componentData.m_PrisonerWellbeing = modeData.m_PrisonerWellbeing;
			componentData.m_PrisonerHealth = modeData.m_PrisonerHealth;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			Prison component = m_ModeDatas[i].m_Prefab.GetComponent<Prison>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PrisonData componentData = entityManager.GetComponentData<PrisonData>(entity);
			componentData.m_PrisonVanCapacity = component.m_PrisonVanCapacity;
			componentData.m_PrisonerWellbeing = component.m_PrisonerWellbeing;
			componentData.m_PrisonerHealth = component.m_PrisonerHealth;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
