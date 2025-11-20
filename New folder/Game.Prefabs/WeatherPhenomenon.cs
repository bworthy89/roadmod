using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Events;
using Game.Rendering;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class WeatherPhenomenon : ComponentBase
{
	public float m_OccurrenceProbability = 10f;

	public Bounds1 m_OccurenceTemperature = new Bounds1(0f, 15f);

	public Bounds1 m_OccurenceRain = new Bounds1(0f, 1f);

	public Bounds1 m_OccurenceCloudiness = new Bounds1(0f, 1f);

	public Bounds1 m_Duration = new Bounds1(15f, 90f);

	public Bounds1 m_PhenomenonRadius = new Bounds1(500f, 1000f);

	public Bounds1 m_HotspotRadius = new Bounds1(0.8f, 0.9f);

	public Bounds1 m_LightningInterval = new Bounds1(0f, 0f);

	[Range(0f, 1f)]
	public float m_HotspotInstability = 0.1f;

	[Range(0f, 100f)]
	public float m_DamageSeverity = 10f;

	[Tooltip("How dangerous the disaster is for the cims in the city. Determines how likely cims will leave shelter while the disaster is ongoing")]
	[Range(0f, 1f)]
	public float m_DangerLevel = 1f;

	public bool m_Evacuate;

	public bool m_StayIndoors;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WeatherPhenomenonData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Events.WeatherPhenomenon>());
		components.Add(ComponentType.ReadWrite<HotspotFrame>());
		components.Add(ComponentType.ReadWrite<Duration>());
		components.Add(ComponentType.ReadWrite<DangerLevel>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
		components.Add(ComponentType.ReadWrite<InterpolatedTransform>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		WeatherPhenomenonData componentData = default(WeatherPhenomenonData);
		componentData.m_OccurenceProbability = m_OccurrenceProbability;
		componentData.m_HotspotInstability = m_HotspotInstability;
		componentData.m_DamageSeverity = m_DamageSeverity;
		componentData.m_DangerLevel = m_DangerLevel;
		componentData.m_PhenomenonRadius = m_PhenomenonRadius;
		componentData.m_HotspotRadius = m_HotspotRadius;
		componentData.m_LightningInterval = m_LightningInterval;
		componentData.m_Duration = m_Duration;
		componentData.m_OccurenceTemperature = m_OccurenceTemperature;
		componentData.m_OccurenceRain = m_OccurenceRain;
		componentData.m_OccurenceCloudiness = m_OccurenceCloudiness;
		componentData.m_DangerFlags = (DangerFlags)0u;
		if (m_Evacuate)
		{
			componentData.m_DangerFlags = DangerFlags.Evacuate;
		}
		if (m_StayIndoors)
		{
			componentData.m_DangerFlags = DangerFlags.StayIndoors;
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
