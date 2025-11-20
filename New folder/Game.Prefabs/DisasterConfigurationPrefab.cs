using System;
using System.Collections.Generic;
using Colossal.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class DisasterConfigurationPrefab : PrefabBase
{
	public NotificationIconPrefab m_WeatherDamageNotificationPrefab;

	public NotificationIconPrefab m_WeatherDestroyedNotificationPrefab;

	public NotificationIconPrefab m_WaterDamageNotificationPrefab;

	public NotificationIconPrefab m_WaterDestroyedNotificationPrefab;

	public NotificationIconPrefab m_DestroyedNotificationPrefab;

	public float m_FloodDamageRate = 200f;

	[Tooltip("Correlation between the general danger level (0.0-1.0) in the city and the probability that the cim will exit the shelter if there is no imminent danger to their home, workplace or school (1024 rolls per day).\nThe y value at 0.0 determines how quickly cims will leave the shelter when there is no danger.")]
	public AnimationCurve m_EmergencyShelterDangerLevelExitProbability;

	[Tooltip("Probability that a cim will exit an inoperable emergency shelter (1024 rolls per day)")]
	[Range(0f, 1f)]
	public float m_InoperableEmergencyShelterExitProbability = 0.1f;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_WeatherDamageNotificationPrefab);
		prefabs.Add(m_WeatherDestroyedNotificationPrefab);
		prefabs.Add(m_WaterDamageNotificationPrefab);
		prefabs.Add(m_WaterDestroyedNotificationPrefab);
		prefabs.Add(m_DestroyedNotificationPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<DisasterConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new DisasterConfigurationData
		{
			m_WeatherDamageNotificationPrefab = existingSystemManaged.GetEntity(m_WeatherDamageNotificationPrefab),
			m_WeatherDestroyedNotificationPrefab = existingSystemManaged.GetEntity(m_WeatherDestroyedNotificationPrefab),
			m_WaterDamageNotificationPrefab = existingSystemManaged.GetEntity(m_WaterDamageNotificationPrefab),
			m_WaterDestroyedNotificationPrefab = existingSystemManaged.GetEntity(m_WaterDestroyedNotificationPrefab),
			m_DestroyedNotificationPrefab = existingSystemManaged.GetEntity(m_DestroyedNotificationPrefab),
			m_FloodDamageRate = m_FloodDamageRate,
			m_EmergencyShelterDangerLevelExitProbability = new AnimationCurve1(m_EmergencyShelterDangerLevelExitProbability),
			m_InoperableEmergencyShelterExitProbability = m_InoperableEmergencyShelterExitProbability
		});
	}
}
