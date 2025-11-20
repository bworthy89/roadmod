using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class PollutionPrefab : PrefabBase
{
	public float m_GroundMultiplier = 25f;

	public float m_AirMultiplier = 25f;

	public float m_NoiseMultiplier = 100f;

	public float m_NetAirMultiplier = 25f;

	public float m_NetNoiseMultiplier = 100f;

	public float m_GroundRadius = 150f;

	public float m_AirRadius = 75f;

	public float m_NoiseRadius = 200f;

	public float m_NetNoiseRadius = 50f;

	public float m_WindAdvectionSpeed = 8f;

	public short m_AirFade = 5;

	public short m_GroundFade = 10;

	public float m_PlantAirMultiplier = 0.001f;

	public float m_PlantGroundMultiplier = 0.001f;

	public float m_PlantFade = 2f;

	public float m_FertilityGroundMultiplier = 1f;

	public float m_DistanceExponent = 2f;

	public NotificationIconPrefab m_AirPollutionNotification;

	public NotificationIconPrefab m_NoisePollutionNotification;

	public NotificationIconPrefab m_GroundPollutionNotification;

	[Tooltip("If happiness effect from air pollution is less than this, show notification")]
	public int m_AirPollutionNotificationLimit = -5;

	[Tooltip("If happiness effect from noise pollution is less than this, show notification")]
	public int m_NoisePollutionNotificationLimit = -5;

	[Tooltip("If happiness effect from ground pollution is less than this, show notification")]
	public int m_GroundPollutionNotificationLimit = -5;

	public float m_AbandonedNoisePollutionMultiplier = 5f;

	public int m_HomelessNoisePollution = 100;

	[Tooltip("The divisor is that pollution value divide this will be the negative affect to land value")]
	public int m_GroundPollutionLandValueDivisor = 500;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PollutionParameterData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new PollutionParameterData
		{
			m_GroundMultiplier = m_GroundMultiplier,
			m_AirMultiplier = m_AirMultiplier,
			m_NoiseMultiplier = m_NoiseMultiplier,
			m_NetAirMultiplier = m_NetAirMultiplier,
			m_NetNoiseMultiplier = m_NetNoiseMultiplier,
			m_GroundRadius = m_GroundRadius,
			m_AirRadius = m_AirRadius,
			m_NoiseRadius = m_NoiseRadius,
			m_NetNoiseRadius = m_NetNoiseRadius,
			m_WindAdvectionSpeed = m_WindAdvectionSpeed,
			m_AirFade = m_AirFade,
			m_GroundFade = m_GroundFade,
			m_PlantAirMultiplier = m_PlantAirMultiplier,
			m_PlantGroundMultiplier = m_PlantGroundMultiplier,
			m_PlantFade = m_PlantFade,
			m_FertilityGroundMultiplier = m_FertilityGroundMultiplier,
			m_DistanceExponent = m_DistanceExponent,
			m_AirPollutionNotification = orCreateSystemManaged.GetEntity(m_AirPollutionNotification),
			m_NoisePollutionNotification = orCreateSystemManaged.GetEntity(m_NoisePollutionNotification),
			m_GroundPollutionNotification = orCreateSystemManaged.GetEntity(m_GroundPollutionNotification),
			m_AirPollutionNotificationLimit = m_AirPollutionNotificationLimit,
			m_NoisePollutionNotificationLimit = m_NoisePollutionNotificationLimit,
			m_GroundPollutionNotificationLimit = m_GroundPollutionNotificationLimit,
			m_AbandonedNoisePollutionMultiplier = m_AbandonedNoisePollutionMultiplier,
			m_HomelessNoisePollution = m_HomelessNoisePollution,
			m_GroundPollutionLandValueDivisor = m_GroundPollutionLandValueDivisor
		});
	}
}
