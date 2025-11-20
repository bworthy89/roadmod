using System;
using System.Collections.Generic;
using Colossal.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class BuildingEfficiencyParametersPrefab : PrefabBase
{
	[Tooltip("How the service budget (between 0.5 and 1.5) correlates the service budget efficiency factor")]
	public AnimationCurve m_ServiceBudgetEfficiencyFactor;

	[Tooltip("If the building efficiency drops below this threshold, the 'low efficiency' flag is applied, which disables certain vfx/sfx")]
	[Range(0f, 1f)]
	public float m_LowEfficiencyThreshold = 0.15f;

	[Header("Electricity")]
	[Tooltip("Efficiency penalty when not enough electricity is supplied")]
	[Range(0f, 1f)]
	public float m_ElectricityPenalty = 0.5f;

	[Tooltip("Interval in ticks until no electricity efficiency penalty is fully applied. One tick equals ~1.42 ingame minutes.")]
	[Min(1f)]
	public short m_ElectricityPenaltyDelay = 32;

	[Tooltip("Defines how the electricity fee efficiency factor correlates to the electricity fee (0-200%)")]
	public AnimationCurve m_ElectricityFeeFactor;

	[Header("Water & Sewage")]
	[Tooltip("Efficiency penalty when not enough water is supplied")]
	[Range(0f, 1f)]
	public float m_WaterPenalty = 0.5f;

	[Tooltip("Delay in ticks until no water efficiency penalty is fully applied. One tick equals ~1.42 ingame minutes.")]
	[Min(1f)]
	public byte m_WaterPenaltyDelay = 32;

	[Tooltip("Efficiency penalty when supplied fresh water is dirty")]
	[Range(0f, 1f)]
	public float m_WaterPollutionPenalty = 0.5f;

	[Tooltip("Efficiency penalty when sewage is not handled")]
	[Range(0f, 1f)]
	public float m_SewagePenalty = 0.5f;

	[Tooltip("Delay in ticks until no sewage efficiency penalty is fully applied. One tick equals ~1.42 ingame minutes.")]
	[Min(1f)]
	public byte m_SewagePenaltyDelay = 32;

	[Tooltip("Defines how the water fee efficiency factor correlates to the water fee (0-200%)")]
	public AnimationCurve m_WaterFeeFactor;

	[Header("Garbage")]
	[Tooltip("Efficiency penalty when garbage has accumulated")]
	[Range(0f, 1f)]
	public float m_GarbagePenalty = 0.5f;

	[Header("Communications")]
	[Tooltip("Amount of mail that is tolerated by the building before efficiency drops")]
	[Min(0f)]
	public int m_NegligibleMail = 20;

	[Tooltip("Efficiency penalty when too much mail is accumulated")]
	[Range(0f, 1f)]
	public float m_MailEfficiencyPenalty = 0.1f;

	[Tooltip("Minimum telecom network quality required before telecom efficiency penalty is applied")]
	[Range(0f, 1f)]
	public float m_TelecomBaseline = 0.3f;

	[Header("Work Provider")]
	[Tooltip("Efficiency penalty when the building has no employees (scales proportionally)")]
	[Range(0f, 1f)]
	public float m_MissingEmployeesEfficiencyPenalty = 0.9f;

	[Tooltip("Delay in ticks until 'not enough employees' penalty is fully applied (512 ticks per day)")]
	[Min(1f)]
	public short m_MissingEmployeesEfficiencyDelay = 16;

	[Tooltip("Extra grace period in ticks for service buildings before 'not enough employees' efficiency starts dropping (512 ticks per day)")]
	[Min(0f)]
	public short m_ServiceBuildingEfficiencyGracePeriod = 16;

	[Tooltip("Efficiency penalty when all employees are sick (scales proportionally)")]
	[Range(0f, 1f)]
	public float m_SickEmployeesEfficiencyPenalty = 0.9f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<BuildingEfficiencyParameterData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new BuildingEfficiencyParameterData
		{
			m_ServiceBudgetEfficiencyFactor = new AnimationCurve1(m_ServiceBudgetEfficiencyFactor),
			m_LowEfficiencyThreshold = m_LowEfficiencyThreshold,
			m_ElectricityPenalty = m_ElectricityPenalty,
			m_ElectricityPenaltyDelay = m_ElectricityPenaltyDelay,
			m_ElectricityFeeFactor = new AnimationCurve1(m_ElectricityFeeFactor),
			m_WaterPenalty = m_WaterPenalty,
			m_WaterPenaltyDelay = (int)m_WaterPenaltyDelay,
			m_WaterPollutionPenalty = m_WaterPollutionPenalty,
			m_SewagePenalty = m_SewagePenalty,
			m_SewagePenaltyDelay = (int)m_SewagePenaltyDelay,
			m_WaterFeeFactor = new AnimationCurve1(m_WaterFeeFactor),
			m_GarbagePenalty = m_GarbagePenalty,
			m_NegligibleMail = m_NegligibleMail,
			m_MailEfficiencyPenalty = m_MailEfficiencyPenalty,
			m_TelecomBaseline = m_TelecomBaseline,
			m_MissingEmployeesEfficiencyPenalty = m_MissingEmployeesEfficiencyPenalty,
			m_MissingEmployeesEfficiencyDelay = m_MissingEmployeesEfficiencyDelay,
			m_ServiceBuildingEfficiencyGracePeriod = m_ServiceBuildingEfficiencyGracePeriod,
			m_SickEmployeesEfficiencyPenalty = m_SickEmployeesEfficiencyPenalty
		});
	}
}
