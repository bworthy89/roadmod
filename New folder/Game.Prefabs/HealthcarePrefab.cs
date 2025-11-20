using System;
using System.Collections.Generic;
using Colossal.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class HealthcarePrefab : PrefabBase
{
	public PrefabBase m_HealthcareServicePrefab;

	public NotificationIconPrefab m_AmbulanceNotificationPrefab;

	public NotificationIconPrefab m_HearseNotificationPrefab;

	public NotificationIconPrefab m_FacilityFullNotificationPrefab;

	[Tooltip("Healthcare transporting notification time in seconds")]
	public float m_TransportWarningTime = 15f;

	[Range(0f, 1f)]
	public float m_NoResourceTreatmentPenalty = 0.5f;

	public float m_BuildingDestoryDeathRate = 0.5f;

	public AnimationCurve m_DeathRate;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_HealthcareServicePrefab);
		prefabs.Add(m_AmbulanceNotificationPrefab);
		prefabs.Add(m_HearseNotificationPrefab);
		prefabs.Add(m_FacilityFullNotificationPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<HealthcareParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new HealthcareParameterData
		{
			m_HealthcareServicePrefab = orCreateSystemManaged.GetEntity(m_HealthcareServicePrefab),
			m_AmbulanceNotificationPrefab = orCreateSystemManaged.GetEntity(m_AmbulanceNotificationPrefab),
			m_HearseNotificationPrefab = orCreateSystemManaged.GetEntity(m_HearseNotificationPrefab),
			m_FacilityFullNotificationPrefab = orCreateSystemManaged.GetEntity(m_FacilityFullNotificationPrefab),
			m_TransportWarningTime = m_TransportWarningTime,
			m_NoResourceTreatmentPenalty = m_NoResourceTreatmentPenalty,
			m_BuildingDestoryDeathRate = m_BuildingDestoryDeathRate,
			m_DeathRate = new AnimationCurve1(m_DeathRate)
		});
	}
}
