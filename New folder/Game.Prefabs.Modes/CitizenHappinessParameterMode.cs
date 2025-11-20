using System;
using Colossal.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class CitizenHappinessParameterMode : EntityQueryModePrefab
{
	[Header("Pollution")]
	public int m_PollutionBonusDivisor;

	public int m_MaxAirAndGroundPollutionBonus;

	public int m_MaxNoisePollutionBonus;

	[Header("Electricity")]
	public float m_ElectricityWellbeingPenaltyMultiplier;

	[Min(1f)]
	public byte m_ElectricityPenaltyDelayMultiplier;

	public AnimationCurve m_ElectricityFeeWellbeingEffect;

	[Header("Water & Sewage")]
	public int m_WaterHealthPenaltyMultiplier;

	public int m_WaterWellbeingPenaltyMultiplier;

	[Min(1f)]
	public byte m_WaterPenaltyDelayMultiplier;

	public float m_WaterPollutionMultiplierOverriden;

	public int m_SewageHealthEffectMultiplier;

	public int m_SewageWellbeingEffectMultiplier;

	[Min(1f)]
	public byte m_SewagePenaltyDelayMultiplier;

	public AnimationCurve m_WaterFeeHealthEffect;

	public AnimationCurve m_WaterFeeWellbeingEffect;

	[Header("Other")]
	public int m_HealthProblemHealthPenalty;

	public int m_DeathWellbeingPenalty;

	public int m_DeathHealthPenalty;

	public int m_LowWellbeing;

	public int m_LowHealth;

	public int m_PenaltyEffect;

	public int m_HomelessHealthEffect;

	public int m_HomelessWellbeingEffect;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<CitizenHappinessParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<CitizenHappinessParameterData>(entity);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		CitizenHappinessPrefab citizenHappinessPrefab = prefabSystem.GetPrefab<CitizenHappinessPrefab>(entity);
		CitizenHappinessParameterData componentData = entityManager.GetComponentData<CitizenHappinessParameterData>(entity);
		componentData.m_PollutionBonusDivisor = citizenHappinessPrefab.m_PollutionDivisor;
		componentData.m_MaxAirAndGroundPollutionBonus = citizenHappinessPrefab.m_MaxAirAndGroundPollution;
		componentData.m_MaxNoisePollutionBonus = citizenHappinessPrefab.m_MaxNoisePollution;
		componentData.m_ElectricityWellbeingPenalty = citizenHappinessPrefab.m_ElectricityWellbeingPenalty;
		componentData.m_ElectricityPenaltyDelay = (int)citizenHappinessPrefab.m_ElectricityPenaltyDelay;
		componentData.m_ElectricityFeeWellbeingEffect = new AnimationCurve1(citizenHappinessPrefab.m_ElectricityFeeWellbeingEffect);
		componentData.m_WaterHealthPenalty = citizenHappinessPrefab.m_WaterHealthPenalty;
		componentData.m_WaterWellbeingPenalty = citizenHappinessPrefab.m_WaterWellbeingPenalty;
		componentData.m_WaterPenaltyDelay = (int)citizenHappinessPrefab.m_WaterPenaltyDelay;
		componentData.m_SewageHealthEffect = citizenHappinessPrefab.m_SewageHealthEffect;
		componentData.m_SewageWellbeingEffect = citizenHappinessPrefab.m_SewageWellbeingEffect;
		componentData.m_SewagePenaltyDelay = (int)citizenHappinessPrefab.m_SewagePenaltyDelay;
		componentData.m_WaterPollutionBonusMultiplier = citizenHappinessPrefab.m_WaterPollutionMultiplier;
		componentData.m_WaterFeeHealthEffect = new AnimationCurve1(citizenHappinessPrefab.m_WaterFeeHealthEffect);
		componentData.m_WaterFeeWellbeingEffect = new AnimationCurve1(citizenHappinessPrefab.m_WaterFeeWellbeingEffect);
		componentData.m_HealthProblemHealthPenalty = citizenHappinessPrefab.m_HealthProblemHealthPenalty;
		componentData.m_DeathWellbeingPenalty = citizenHappinessPrefab.m_DeathWellbeingPenalty;
		componentData.m_DeathHealthPenalty = citizenHappinessPrefab.m_DeathHealthPenalty;
		componentData.m_LowWellbeing = citizenHappinessPrefab.m_LowWellbeing;
		componentData.m_LowHealth = citizenHappinessPrefab.m_LowHealth;
		componentData.m_PenaltyEffect = citizenHappinessPrefab.m_PenaltyEffect;
		componentData.m_HomelessHealthEffect = citizenHappinessPrefab.m_HomelessHealthEffect;
		componentData.m_HomelessWellbeingEffect = citizenHappinessPrefab.m_HomelessWellbeingEffect;
		entityManager.SetComponentData(entity, componentData);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		CitizenHappinessParameterData componentData = entityManager.GetComponentData<CitizenHappinessParameterData>(singletonEntity);
		componentData.m_PollutionBonusDivisor = m_PollutionBonusDivisor;
		componentData.m_MaxAirAndGroundPollutionBonus = m_MaxAirAndGroundPollutionBonus;
		componentData.m_MaxNoisePollutionBonus = m_MaxNoisePollutionBonus;
		componentData.m_ElectricityWellbeingPenalty = m_ElectricityWellbeingPenaltyMultiplier;
		componentData.m_ElectricityPenaltyDelay = (int)m_ElectricityPenaltyDelayMultiplier;
		componentData.m_ElectricityFeeWellbeingEffect = new AnimationCurve1(m_ElectricityFeeWellbeingEffect);
		componentData.m_WaterHealthPenalty = m_WaterHealthPenaltyMultiplier;
		componentData.m_WaterWellbeingPenalty = m_WaterWellbeingPenaltyMultiplier;
		componentData.m_WaterPenaltyDelay = (int)m_WaterPenaltyDelayMultiplier;
		componentData.m_SewageHealthEffect = m_SewageHealthEffectMultiplier;
		componentData.m_SewageWellbeingEffect = m_SewageWellbeingEffectMultiplier;
		componentData.m_SewagePenaltyDelay = (int)m_SewagePenaltyDelayMultiplier;
		componentData.m_WaterPollutionBonusMultiplier = m_WaterPollutionMultiplierOverriden;
		componentData.m_WaterFeeHealthEffect = new AnimationCurve1(m_WaterFeeHealthEffect);
		componentData.m_WaterFeeWellbeingEffect = new AnimationCurve1(m_WaterFeeWellbeingEffect);
		componentData.m_HealthProblemHealthPenalty = m_HealthProblemHealthPenalty;
		componentData.m_DeathWellbeingPenalty = m_DeathWellbeingPenalty;
		componentData.m_DeathHealthPenalty = m_DeathHealthPenalty;
		componentData.m_LowWellbeing = m_LowWellbeing;
		componentData.m_LowHealth = m_LowHealth;
		componentData.m_PenaltyEffect = m_PenaltyEffect;
		componentData.m_HomelessHealthEffect = m_HomelessHealthEffect;
		componentData.m_HomelessWellbeingEffect = m_HomelessWellbeingEffect;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}
}
