using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class EducationParametersMode : EntityQueryModePrefab
{
	[Range(0f, 1f)]
	public float m_InoperableSchoolLeaveProbability;

	[Range(0f, 1f)]
	public float m_EnterHighSchoolProbability;

	[Range(0f, 1f)]
	public float m_AdultEnterHighSchoolProbability;

	[Range(0f, 1f)]
	public float m_WorkerContinueEducationProbability;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<EducationParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<EducationParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		EducationParameterData componentData = entityManager.GetComponentData<EducationParameterData>(singletonEntity);
		componentData.m_InoperableSchoolLeaveProbability = m_InoperableSchoolLeaveProbability;
		componentData.m_EnterHighSchoolProbability = m_EnterHighSchoolProbability;
		componentData.m_AdultEnterHighSchoolProbability = m_AdultEnterHighSchoolProbability;
		componentData.m_WorkerContinueEducationProbability = m_WorkerContinueEducationProbability;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		EducationPrefab educationPrefab = prefabSystem.GetPrefab<EducationPrefab>(entity);
		EducationParameterData componentData = entityManager.GetComponentData<EducationParameterData>(entity);
		componentData.m_InoperableSchoolLeaveProbability = educationPrefab.m_InoperableSchoolLeaveProbability;
		componentData.m_EnterHighSchoolProbability = educationPrefab.m_EnterHighSchoolProbability;
		componentData.m_AdultEnterHighSchoolProbability = educationPrefab.m_AdultEnterHighSchoolProbability;
		componentData.m_WorkerContinueEducationProbability = educationPrefab.m_WorkerContinueEducationProbability;
		entityManager.SetComponentData(entity, componentData);
	}
}
