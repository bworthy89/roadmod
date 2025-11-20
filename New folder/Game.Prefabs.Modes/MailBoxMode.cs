using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class MailBoxMode : LocalModePrefab
{
	[Serializable]
	public class ModeData
	{
		public ObjectPrefab m_Prefab;

		public float m_MailCapacityMultifier;

		public float m_ComfortFactor;
	}

	public ModeData[] m_ModeDatas;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			MailBox component = m_ModeDatas[i].m_Prefab.GetComponent<MailBox>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			entityManager.GetComponentData<MailBoxData>(entity);
			if (entityManager.HasComponent<TransportStopData>(entity))
			{
				entityManager.GetComponentData<TransportStopData>(entity);
			}
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			ModeData modeData = m_ModeDatas[i];
			MailBox component = modeData.m_Prefab.GetComponent<MailBox>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			MailBoxData componentData = entityManager.GetComponentData<MailBoxData>(entity);
			componentData.m_MailCapacity = (int)((float)componentData.m_MailCapacity * modeData.m_MailCapacityMultifier);
			entityManager.SetComponentData(entity, componentData);
			if (entityManager.HasComponent<TransportStopData>(entity))
			{
				TransportStopData componentData2 = entityManager.GetComponentData<TransportStopData>(entity);
				componentData2.m_ComfortFactor = modeData.m_ComfortFactor;
				entityManager.SetComponentData(entity, componentData2);
			}
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_ModeDatas.Length; i++)
		{
			MailBox component = m_ModeDatas[i].m_Prefab.GetComponent<MailBox>();
			if (component == null)
			{
				ComponentBase.baseLog.Critical($"Target not found {this}");
				continue;
			}
			Entity entity = prefabSystem.GetEntity(component.prefab);
			MailBoxData componentData = entityManager.GetComponentData<MailBoxData>(entity);
			componentData.m_MailCapacity = component.m_MailCapacity;
			entityManager.SetComponentData(entity, componentData);
			if (entityManager.HasComponent<TransportStopData>(entity))
			{
				TransportStopData componentData2 = entityManager.GetComponentData<TransportStopData>(entity);
				componentData2.m_ComfortFactor = component.m_ComfortFactor;
				entityManager.SetComponentData(entity, componentData2);
			}
		}
	}
}
