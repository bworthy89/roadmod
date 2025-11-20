using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class GarbageFacilityMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_GarbageCapacityMultiplier;

		public float m_ProcessingSpeedMultiplier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			GarbageFacility component = m_ModeDatas[i].m_Prefab.GetComponent<GarbageFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<GarbageFacilityData>(entity);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			GarbageFacility component = modeData.m_Prefab.GetComponent<GarbageFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			GarbageFacilityData componentData = entityManager.GetComponentData<GarbageFacilityData>(entity);
			componentData.m_GarbageCapacity = (int)((float)componentData.m_GarbageCapacity * modeData.m_GarbageCapacityMultiplier);
			componentData.m_ProcessingSpeed = (int)((float)componentData.m_ProcessingSpeed * modeData.m_ProcessingSpeedMultiplier);
			entityManager.SetComponentData(entity, componentData);
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			GarbageFacility component = m_ModeDatas[i].m_Prefab.GetComponent<GarbageFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			GarbageFacilityData componentData = entityManager.GetComponentData<GarbageFacilityData>(entity);
			componentData.m_GarbageCapacity = component.m_GarbageCapacity;
			componentData.m_ProcessingSpeed = component.m_ProcessingSpeed;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
