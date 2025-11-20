using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class CitizenParametersPrefab : PrefabBase
{
	[Tooltip("The divorce rate in percentage, 0.16 means 16%")]
	public float m_DivorceRate = 0.16f;

	[Tooltip("The single citizen look for partner rate in percentage, 0.08 means 8%")]
	public float m_LookForPartnerRate = 0.08f;

	[Tooltip("Partner type rate, x-Same Gender, y-Any Gender, 1-x-y = Different Gender")]
	public float2 m_LookForPartnerTypeRate = new float2(0.04f, 0.1f);

	[Tooltip("The base birth rate in percentage, 0.02 means 2%")]
	public float m_BaseBirthRate = 0.02f;

	[Tooltip("The birth rate bonus to adult female gender, for example 0.08 means female adult is (base:0.02 + bonus:0.08) = 0.1(10%)")]
	public float m_AdultFemaleBirthRateBonus = 0.08f;

	[Tooltip("The birth rate adjust for students, (final birth rate) * (adjust), for example 0.5 means student only have half of the final birth rate)")]
	public float m_StudentBirthRateAdjust = 0.5f;

	[Tooltip("The switch job check (current have job) rate in percentage, also need to do LookForNewJobEmployableRate check next, 0.032 means 3.2%")]
	public float m_SwitchJobRate = 0.032f;

	[Tooltip("The rate is the free workplace compare to the employable workers, for example 2 means that there are twice amount of free work position of the employable workers, which means 50%, skip if free position < random(rate * employable workers)")]
	public float m_LookForNewJobEmployableRate = 2f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CitizenParametersData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new CitizenParametersData
		{
			m_DivorceRate = m_DivorceRate,
			m_LookForPartnerRate = m_LookForPartnerRate,
			m_LookForPartnerTypeRate = m_LookForPartnerTypeRate,
			m_BaseBirthRate = m_BaseBirthRate,
			m_AdultFemaleBirthRateBonus = m_AdultFemaleBirthRateBonus,
			m_StudentBirthRateAdjust = m_StudentBirthRateAdjust,
			m_SwitchJobRate = m_SwitchJobRate,
			m_LookForNewJobEmployableRate = m_LookForNewJobEmployableRate
		});
	}
}
