using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class PoliceConfigurationMode : EntityQueryModePrefab
{
	public float m_MaxCrimeAccumulationMultiplier;

	public float m_CrimeAccumulationToleranceMultiplier;

	public int m_HomeCrimeEffectMultiplier;

	public int m_WorkplaceCrimeEffectMultiplier;

	public float m_WelfareCrimeRecurrenceFactor;

	public float m_CrimePoliceCoverageFactorMultiflier;

	public float m_CrimePopulationReductionMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<PoliceConfigurationData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<PoliceConfigurationData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		PoliceConfigurationData componentData = entityManager.GetComponentData<PoliceConfigurationData>(singletonEntity);
		componentData.m_MaxCrimeAccumulation *= m_MaxCrimeAccumulationMultiplier;
		componentData.m_CrimeAccumulationTolerance *= m_CrimeAccumulationToleranceMultiplier;
		componentData.m_HomeCrimeEffect *= m_HomeCrimeEffectMultiplier;
		componentData.m_WorkplaceCrimeEffect *= m_WorkplaceCrimeEffectMultiplier;
		componentData.m_WelfareCrimeRecurrenceFactor = m_WelfareCrimeRecurrenceFactor;
		componentData.m_CrimePoliceCoverageFactor *= m_CrimePoliceCoverageFactorMultiflier;
		componentData.m_CrimePopulationReduction *= m_CrimePopulationReductionMultiplier;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		PoliceConfigurationPrefab policeConfigurationPrefab = prefabSystem.GetPrefab<PoliceConfigurationPrefab>(entity);
		PoliceConfigurationData componentData = entityManager.GetComponentData<PoliceConfigurationData>(entity);
		componentData.m_MaxCrimeAccumulation = policeConfigurationPrefab.m_MaxCrimeAccumulation;
		componentData.m_CrimeAccumulationTolerance = policeConfigurationPrefab.m_CrimeAccumulationTolerance;
		componentData.m_HomeCrimeEffect = policeConfigurationPrefab.m_HomeCrimeEffect;
		componentData.m_WorkplaceCrimeEffect = policeConfigurationPrefab.m_WorkplaceCrimeEffect;
		componentData.m_WelfareCrimeRecurrenceFactor = policeConfigurationPrefab.m_WelfareCrimeRecurrenceFactor;
		componentData.m_CrimePoliceCoverageFactor = policeConfigurationPrefab.m_CrimePoliceCoverageFactor;
		componentData.m_CrimePopulationReduction = policeConfigurationPrefab.m_CrimePopulationReduction;
		entityManager.SetComponentData(entity, componentData);
	}
}
