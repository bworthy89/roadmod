using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class WorkProviderParameterMode : EntityQueryModePrefab
{
	[Min(1f)]
	public short m_UneducatedNotificationDelay;

	[Min(1f)]
	public short m_EducatedNotificationDelay;

	[Range(0f, 1f)]
	public float m_UneducatedNotificationLimit;

	[Range(0f, 1f)]
	public float m_EducatedNotificationLimit;

	public int m_SeniorEmployeeLevel;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<WorkProviderParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<WorkProviderParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		WorkProviderParameterData componentData = entityManager.GetComponentData<WorkProviderParameterData>(singletonEntity);
		componentData.m_UneducatedNotificationDelay = m_UneducatedNotificationDelay;
		componentData.m_EducatedNotificationDelay = m_EducatedNotificationDelay;
		componentData.m_UneducatedNotificationLimit = m_UneducatedNotificationLimit;
		componentData.m_EducatedNotificationLimit = m_EducatedNotificationLimit;
		componentData.m_SeniorEmployeeLevel = m_SeniorEmployeeLevel;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		WorkProviderParameterPrefab workProviderParameterPrefab = prefabSystem.GetPrefab<WorkProviderParameterPrefab>(entity);
		WorkProviderParameterData componentData = entityManager.GetComponentData<WorkProviderParameterData>(entity);
		componentData.m_UneducatedNotificationDelay = workProviderParameterPrefab.m_UneducatedNotificationDelay;
		componentData.m_EducatedNotificationDelay = workProviderParameterPrefab.m_EducatedNotificationDelay;
		componentData.m_UneducatedNotificationLimit = workProviderParameterPrefab.m_UneducatedNotificationLimit;
		componentData.m_EducatedNotificationLimit = workProviderParameterPrefab.m_EducatedNotificationLimit;
		componentData.m_SeniorEmployeeLevel = workProviderParameterPrefab.m_SeniorEmployeeLevel;
		entityManager.SetComponentData(entity, componentData);
	}
}
