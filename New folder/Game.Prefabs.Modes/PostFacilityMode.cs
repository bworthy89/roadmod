using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class PostFacilityMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public PrefabBase m_Prefab;

		public float m_MailStorageCapacityMultifier;

		public float m_MailBoxCapacityMultifier;

		public float m_SortingRateMultifier;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			PostFacility component = m_ModeDatas[i].m_Prefab.GetComponent<PostFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<PostFacilityData>(entity);
			if (entityManager.HasComponent<MailBoxData>(entity))
			{
				entityManager.GetComponentData<MailBoxData>(entity);
			}
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			PostFacility component = modeData.m_Prefab.GetComponent<PostFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PostFacilityData componentData = entityManager.GetComponentData<PostFacilityData>(entity);
			componentData.m_MailCapacity = (int)((float)componentData.m_MailCapacity * modeData.m_MailStorageCapacityMultifier);
			componentData.m_SortingRate = (int)((float)componentData.m_SortingRate * modeData.m_SortingRateMultifier);
			entityManager.SetComponentData(entity, componentData);
			if (entityManager.HasComponent<MailBoxData>(entity))
			{
				MailBoxData componentData2 = entityManager.GetComponentData<MailBoxData>(entity);
				componentData2.m_MailCapacity = (int)((float)componentData2.m_MailCapacity * modeData.m_MailBoxCapacityMultifier);
				entityManager.SetComponentData(entity, componentData2);
			}
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			PostFacility component = m_ModeDatas[i].m_Prefab.GetComponent<PostFacility>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			PostFacilityData componentData = entityManager.GetComponentData<PostFacilityData>(entity);
			componentData.m_MailCapacity = component.m_MailStorageCapacity;
			componentData.m_SortingRate = component.m_SortingRate;
			entityManager.SetComponentData(entity, componentData);
			if (entityManager.HasComponent<MailBoxData>(entity))
			{
				MailBoxData componentData2 = entityManager.GetComponentData<MailBoxData>(entity);
				componentData2.m_MailCapacity = component.m_MailBoxCapacity;
				entityManager.SetComponentData(entity, componentData2);
			}
		}
	}
}
