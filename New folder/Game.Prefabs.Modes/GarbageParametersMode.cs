using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class GarbageParametersMode : EntityQueryModePrefab
{
	public int m_HomelessGarbageProduce = 25;

	public int m_CollectionGarbageLimit = 20;

	public int m_RequestGarbageLimit = 100;

	public int m_WarningGarbageLimit = 500;

	public int m_MaxGarbageAccumulation = 2000;

	public float m_BuildingLevelBalance = 1.25f;

	public float m_EducationBalance = 2.5f;

	public int m_HappinessEffectBaseline = 100;

	public int m_HappinessEffectStep = 65;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<GarbageParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<GarbageParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		GarbageParameterData componentData = entityManager.GetComponentData<GarbageParameterData>(singletonEntity);
		componentData.m_HomelessGarbageProduce = m_HomelessGarbageProduce;
		componentData.m_CollectionGarbageLimit = m_CollectionGarbageLimit;
		componentData.m_RequestGarbageLimit = m_RequestGarbageLimit;
		componentData.m_WarningGarbageLimit = m_WarningGarbageLimit;
		componentData.m_MaxGarbageAccumulation = m_MaxGarbageAccumulation;
		componentData.m_BuildingLevelBalance = m_BuildingLevelBalance;
		componentData.m_EducationBalance = m_EducationBalance;
		componentData.m_HappinessEffectBaseline = m_HappinessEffectBaseline;
		componentData.m_HappinessEffectStep = m_HappinessEffectStep;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		GarbagePrefab garbagePrefab = prefabSystem.GetPrefab<GarbagePrefab>(entity);
		GarbageParameterData componentData = entityManager.GetComponentData<GarbageParameterData>(entity);
		componentData.m_HomelessGarbageProduce = garbagePrefab.m_HomelessGarbageProduce;
		componentData.m_CollectionGarbageLimit = garbagePrefab.m_CollectionGarbageLimit;
		componentData.m_RequestGarbageLimit = garbagePrefab.m_RequestGarbageLimit;
		componentData.m_WarningGarbageLimit = garbagePrefab.m_WarningGarbageLimit;
		componentData.m_MaxGarbageAccumulation = garbagePrefab.m_MaxGarbageAccumulation;
		componentData.m_BuildingLevelBalance = garbagePrefab.m_BuildingLevelBalance;
		componentData.m_EducationBalance = garbagePrefab.m_EducationBalance;
		componentData.m_HappinessEffectBaseline = garbagePrefab.m_HappinessEffectBaseline;
		componentData.m_HappinessEffectStep = garbagePrefab.m_HappinessEffectStep;
		entityManager.SetComponentData(entity, componentData);
	}
}
