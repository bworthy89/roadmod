using System;
using Colossal.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class BuildingEfficiencyParametersMode : EntityQueryModePrefab
{
	public AnimationCurve m_ServiceBudgetEfficiencyFactor;

	[Range(0f, 1f)]
	public float m_LowEfficiencyThreshold = 0.15f;

	[Header("Electricity")]
	[Range(0f, 1f)]
	public float m_ElectricityPenalty = 0.5f;

	[Min(1f)]
	public short m_ElectricityPenaltyDelay = 32;

	public AnimationCurve m_ElectricityFeeFactor;

	[Header("Water & Sewage")]
	[Range(0f, 1f)]
	public float m_WaterPenalty = 0.5f;

	[Min(1f)]
	public byte m_WaterPenaltyDelay = 32;

	[Range(0f, 1f)]
	public float m_WaterPollutionPenalty = 0.5f;

	[Range(0f, 1f)]
	public float m_SewagePenalty = 0.5f;

	[Min(1f)]
	public byte m_SewagePenaltyDelay = 32;

	public AnimationCurve m_WaterFeeFactor;

	[Header("Garbage")]
	[Range(0f, 1f)]
	public float m_GarbagePenalty = 0.5f;

	[Header("Communications")]
	[Min(0f)]
	public int m_NegligibleMail = 20;

	[Range(0f, 1f)]
	public float m_MailEfficiencyPenalty = 0.1f;

	[Range(0f, 1f)]
	public float m_TelecomBaseline = 0.3f;

	[Header("Work Provider")]
	[Range(0f, 1f)]
	public float m_MissingEmployeesEfficiencyPenalty = 0.9f;

	[Min(1f)]
	public short m_MissingEmployeesEfficiencyDelay = 16;

	[Min(0f)]
	public short m_ServiceBuildingEfficiencyGracePeriod = 16;

	[Range(0f, 1f)]
	public float m_SickEmployeesEfficiencyPenalty = 0.9f;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<BuildingEfficiencyParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<BuildingEfficiencyParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		BuildingEfficiencyParameterData componentData = entityManager.GetComponentData<BuildingEfficiencyParameterData>(singletonEntity);
		componentData.m_ServiceBudgetEfficiencyFactor = new AnimationCurve1(m_ServiceBudgetEfficiencyFactor);
		componentData.m_LowEfficiencyThreshold = m_LowEfficiencyThreshold;
		componentData.m_ElectricityPenalty = m_ElectricityPenalty;
		componentData.m_ElectricityPenaltyDelay = m_ElectricityPenaltyDelay;
		componentData.m_ElectricityFeeFactor = new AnimationCurve1(m_ElectricityFeeFactor);
		componentData.m_WaterPenalty = m_WaterPenalty;
		componentData.m_WaterPenaltyDelay = (int)m_WaterPenaltyDelay;
		componentData.m_WaterPollutionPenalty = m_WaterPollutionPenalty;
		componentData.m_SewagePenalty = m_SewagePenalty;
		componentData.m_SewagePenaltyDelay = (int)m_SewagePenaltyDelay;
		componentData.m_WaterFeeFactor = new AnimationCurve1(m_WaterFeeFactor);
		componentData.m_GarbagePenalty = m_GarbagePenalty;
		componentData.m_NegligibleMail = m_NegligibleMail;
		componentData.m_MailEfficiencyPenalty = m_MailEfficiencyPenalty;
		componentData.m_TelecomBaseline = m_TelecomBaseline;
		componentData.m_MissingEmployeesEfficiencyPenalty = m_MissingEmployeesEfficiencyPenalty;
		componentData.m_MissingEmployeesEfficiencyDelay = m_MissingEmployeesEfficiencyDelay;
		componentData.m_ServiceBuildingEfficiencyGracePeriod = m_ServiceBuildingEfficiencyGracePeriod;
		componentData.m_SickEmployeesEfficiencyPenalty = m_SickEmployeesEfficiencyPenalty;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		BuildingEfficiencyParametersPrefab buildingEfficiencyParametersPrefab = prefabSystem.GetPrefab<BuildingEfficiencyParametersPrefab>(entity);
		BuildingEfficiencyParameterData componentData = entityManager.GetComponentData<BuildingEfficiencyParameterData>(entity);
		componentData.m_ServiceBudgetEfficiencyFactor = new AnimationCurve1(buildingEfficiencyParametersPrefab.m_ServiceBudgetEfficiencyFactor);
		componentData.m_LowEfficiencyThreshold = buildingEfficiencyParametersPrefab.m_LowEfficiencyThreshold;
		componentData.m_ElectricityPenalty = buildingEfficiencyParametersPrefab.m_ElectricityPenalty;
		componentData.m_ElectricityPenaltyDelay = buildingEfficiencyParametersPrefab.m_ElectricityPenaltyDelay;
		componentData.m_ElectricityFeeFactor = new AnimationCurve1(buildingEfficiencyParametersPrefab.m_ElectricityFeeFactor);
		componentData.m_WaterPenalty = buildingEfficiencyParametersPrefab.m_WaterPenalty;
		componentData.m_WaterPenaltyDelay = (int)buildingEfficiencyParametersPrefab.m_WaterPenaltyDelay;
		componentData.m_WaterPollutionPenalty = buildingEfficiencyParametersPrefab.m_WaterPollutionPenalty;
		componentData.m_SewagePenalty = buildingEfficiencyParametersPrefab.m_SewagePenalty;
		componentData.m_SewagePenaltyDelay = (int)buildingEfficiencyParametersPrefab.m_SewagePenaltyDelay;
		componentData.m_WaterFeeFactor = new AnimationCurve1(buildingEfficiencyParametersPrefab.m_WaterFeeFactor);
		componentData.m_GarbagePenalty = buildingEfficiencyParametersPrefab.m_GarbagePenalty;
		componentData.m_NegligibleMail = buildingEfficiencyParametersPrefab.m_NegligibleMail;
		componentData.m_MailEfficiencyPenalty = buildingEfficiencyParametersPrefab.m_MailEfficiencyPenalty;
		componentData.m_TelecomBaseline = buildingEfficiencyParametersPrefab.m_TelecomBaseline;
		componentData.m_MissingEmployeesEfficiencyPenalty = buildingEfficiencyParametersPrefab.m_MissingEmployeesEfficiencyPenalty;
		componentData.m_MissingEmployeesEfficiencyDelay = buildingEfficiencyParametersPrefab.m_MissingEmployeesEfficiencyDelay;
		componentData.m_ServiceBuildingEfficiencyGracePeriod = buildingEfficiencyParametersPrefab.m_ServiceBuildingEfficiencyGracePeriod;
		componentData.m_SickEmployeesEfficiencyPenalty = buildingEfficiencyParametersPrefab.m_SickEmployeesEfficiencyPenalty;
		entityManager.SetComponentData(entity, componentData);
	}
}
