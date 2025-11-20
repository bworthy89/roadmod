using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Prefab/", new Type[] { })]
public class MilestonesMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public MilestonePrefab m_Prefab;

		public int m_Reward;

		public int m_DevTreePoints;

		public int m_MapTiles;

		public int m_LoanLimit;

		public int m_XpRequried;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			MilestonePrefab milestonePrefab = m_ModeDatas[i].m_Prefab;
			if (milestonePrefab == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(milestonePrefab);
			entityManager.GetComponentData<MilestoneData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			MilestonePrefab milestonePrefab = modeData.m_Prefab;
			if (milestonePrefab == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(milestonePrefab);
			MilestoneData componentData = entityManager.GetComponentData<MilestoneData>(entity);
			componentData.m_Reward = modeData.m_Reward;
			componentData.m_DevTreePoints = modeData.m_DevTreePoints;
			componentData.m_MapTiles = modeData.m_MapTiles;
			componentData.m_LoanLimit = modeData.m_LoanLimit;
			componentData.m_XpRequried = modeData.m_XpRequried;
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			MilestonePrefab milestonePrefab = m_ModeDatas[i].m_Prefab;
			if (milestonePrefab == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(milestonePrefab);
			MilestoneData componentData = entityManager.GetComponentData<MilestoneData>(entity);
			componentData.m_Reward = milestonePrefab.m_Reward;
			componentData.m_DevTreePoints = milestonePrefab.m_DevTreePoints;
			componentData.m_MapTiles = milestonePrefab.m_MapTiles;
			componentData.m_LoanLimit = milestonePrefab.m_LoanLimit;
			componentData.m_XpRequried = milestonePrefab.m_XpRequried;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
