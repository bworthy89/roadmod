using System;
using Colossal.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class HealthcareParametersMode : EntityQueryModePrefab
{
	public float m_TransportWarningTime = 15f;

	public float m_NoResourceTreatmentPenalty = 0.5f;

	public float m_BuildingDestoryDeathRate = 0.5f;

	public AnimationCurve m_DeathRate;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<HealthcareParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<HealthcareParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		HealthcareParameterData componentData = entityManager.GetComponentData<HealthcareParameterData>(singletonEntity);
		componentData.m_TransportWarningTime = m_TransportWarningTime;
		componentData.m_NoResourceTreatmentPenalty = m_NoResourceTreatmentPenalty;
		componentData.m_BuildingDestoryDeathRate = m_BuildingDestoryDeathRate;
		componentData.m_DeathRate = new AnimationCurve1(m_DeathRate);
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		HealthcarePrefab healthcarePrefab = prefabSystem.GetPrefab<HealthcarePrefab>(entity);
		HealthcareParameterData componentData = entityManager.GetComponentData<HealthcareParameterData>(entity);
		componentData.m_TransportWarningTime = healthcarePrefab.m_TransportWarningTime;
		componentData.m_NoResourceTreatmentPenalty = healthcarePrefab.m_NoResourceTreatmentPenalty;
		componentData.m_BuildingDestoryDeathRate = healthcarePrefab.m_BuildingDestoryDeathRate;
		componentData.m_DeathRate = new AnimationCurve1(healthcarePrefab.m_DeathRate);
		entityManager.SetComponentData(entity, componentData);
	}
}
