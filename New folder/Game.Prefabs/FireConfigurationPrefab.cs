using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class FireConfigurationPrefab : PrefabBase
{
	public NotificationIconPrefab m_FireNotificationPrefab;

	public NotificationIconPrefab m_BurnedDownNotificationPrefab;

	public float m_DefaultStructuralIntegrity = 3000f;

	public float m_BuildingStructuralIntegrity = 15000f;

	public float m_StructuralIntegrityLevel1 = 12000f;

	public float m_StructuralIntegrityLevel2 = 13000f;

	public float m_StructuralIntegrityLevel3 = 14000f;

	public float m_StructuralIntegrityLevel4 = 15000f;

	public float m_StructuralIntegrityLevel5 = 16000f;

	public Bounds1 m_ResponseTimeRange = new Bounds1(3f, 30f);

	public float m_TelecomResponseTimeModifier = -0.15f;

	public float m_DarknessResponseTimeModifier = 0.1f;

	public AnimationCurve m_TemperatureForestFireHazard;

	public AnimationCurve m_NoRainForestFireHazard;

	public float m_DeathRateOfFireAccident = 0.01f;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_FireNotificationPrefab);
		prefabs.Add(m_BurnedDownNotificationPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<FireConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		FireConfigurationData componentData = default(FireConfigurationData);
		componentData.m_FireNotificationPrefab = existingSystemManaged.GetEntity(m_FireNotificationPrefab);
		componentData.m_BurnedDownNotificationPrefab = existingSystemManaged.GetEntity(m_BurnedDownNotificationPrefab);
		componentData.m_DefaultStructuralIntegrity = m_DefaultStructuralIntegrity;
		componentData.m_BuildingStructuralIntegrity = m_BuildingStructuralIntegrity;
		componentData.m_StructuralIntegrityLevel1 = m_StructuralIntegrityLevel1;
		componentData.m_StructuralIntegrityLevel2 = m_StructuralIntegrityLevel2;
		componentData.m_StructuralIntegrityLevel3 = m_StructuralIntegrityLevel3;
		componentData.m_StructuralIntegrityLevel4 = m_StructuralIntegrityLevel4;
		componentData.m_StructuralIntegrityLevel5 = m_StructuralIntegrityLevel5;
		componentData.m_ResponseTimeRange = m_ResponseTimeRange;
		componentData.m_TelecomResponseTimeModifier = m_TelecomResponseTimeModifier;
		componentData.m_DarknessResponseTimeModifier = m_DarknessResponseTimeModifier;
		componentData.m_DeathRateOfFireAccident = m_DeathRateOfFireAccident;
		entityManager.SetComponentData(entity, componentData);
	}
}
