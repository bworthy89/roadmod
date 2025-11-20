using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class PoliceConfigurationPrefab : PrefabBase
{
	public PrefabBase m_PoliceServicePrefab;

	public NotificationIconPrefab m_TrafficAccidentNotificationPrefab;

	public NotificationIconPrefab m_CrimeSceneNotificationPrefab;

	public float m_MaxCrimeAccumulation = 100000f;

	public float m_CrimeAccumulationTolerance = 1000f;

	public int m_HomeCrimeEffect = 15;

	public int m_WorkplaceCrimeEffect = 5;

	public float m_WelfareCrimeRecurrenceFactor = 0.4f;

	[FormerlySerializedAs("m_CrimeIncreaseMultiplier")]
	[Tooltip("Crime increase factor of police coverage, multiply with the result of formula[ crimeIncrease * (10-coverage) / 10 ], the bigger the crime increase more(less police affect), the smaller the crime increase less(more police affect)")]
	public float m_CrimePoliceCoverageFactor = 2f;

	[Tooltip("Reduce the crime possibility according to the population, the possibility random will be calculated by population / CrimePopulationReduction * 100, and not less than 100")]
	public float m_CrimePopulationReduction = 2000f;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_PoliceServicePrefab);
		prefabs.Add(m_TrafficAccidentNotificationPrefab);
		prefabs.Add(m_CrimeSceneNotificationPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PoliceConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		PoliceConfigurationData componentData = default(PoliceConfigurationData);
		componentData.m_PoliceServicePrefab = orCreateSystemManaged.GetEntity(m_PoliceServicePrefab);
		componentData.m_TrafficAccidentNotificationPrefab = orCreateSystemManaged.GetEntity(m_TrafficAccidentNotificationPrefab);
		componentData.m_CrimeSceneNotificationPrefab = orCreateSystemManaged.GetEntity(m_CrimeSceneNotificationPrefab);
		componentData.m_MaxCrimeAccumulation = m_MaxCrimeAccumulation;
		componentData.m_CrimeAccumulationTolerance = m_CrimeAccumulationTolerance;
		componentData.m_HomeCrimeEffect = m_HomeCrimeEffect;
		componentData.m_WorkplaceCrimeEffect = m_WorkplaceCrimeEffect;
		componentData.m_WelfareCrimeRecurrenceFactor = m_WelfareCrimeRecurrenceFactor;
		componentData.m_CrimePoliceCoverageFactor = m_CrimePoliceCoverageFactor;
		componentData.m_CrimePopulationReduction = m_CrimePopulationReduction;
		entityManager.SetComponentData(entity, componentData);
	}
}
