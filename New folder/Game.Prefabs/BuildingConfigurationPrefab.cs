using System;
using System.Collections.Generic;
using Game.Economy;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class BuildingConfigurationPrefab : PrefabBase
{
	[Tooltip("The building condition increase when building received enough upkeep cost, change 16 times per game day, x-residential, y-commercial, z-industrial")]
	public int3 m_BuildingConditionIncrement = new int3(30, 30, 30);

	[Tooltip("The building condition decrease when building can't pay enough upkeep cost, change 16 times per game day")]
	public int m_BuildingConditionDecrement = 1;

	public LocalEffectsPrefab m_AbandonedBuildingLocalEffects;

	public LocalEffectsPrefab m_AbandonedCollapsedBuildingLocalEffects;

	public NotificationIconPrefab m_AbandonedCollapsedNotification;

	public NotificationIconPrefab m_AbandonedNotification;

	public NotificationIconPrefab m_CondemnedNotification;

	public NotificationIconPrefab m_LevelUpNotification;

	public NotificationIconPrefab m_TurnedOffNotification;

	public NetLanePrefab m_ElectricityConnectionLane;

	public NetLanePrefab m_SewageConnectionLane;

	public NetLanePrefab m_WaterConnectionLane;

	public uint m_AbandonedDestroyDelay;

	public NotificationIconPrefab m_HighRentNotification;

	public BrandPrefab m_DefaultRenterBrand;

	public AreaPrefab m_ConstructionSurface;

	public NetLanePrefab m_ConstructionBorder;

	public ObjectPrefab m_ConstructionObject;

	public ObjectPrefab m_CollapsedObject;

	public EffectPrefab m_CollapseVFX;

	public EffectPrefab m_CollapseSFX;

	public float m_CollapseSFXDensity = 0.1f;

	public AreaPrefab m_CollapsedSurface;

	public EffectPrefab m_FireLoopSFX;

	public EffectPrefab m_FireSpotSFX;

	public LevelUpResource[] m_LevelUpResources;

	public NotificationIconPrefab m_LevelingBuildingNotificationPrefab;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_AbandonedBuildingLocalEffects);
		prefabs.Add(m_AbandonedCollapsedBuildingLocalEffects);
		prefabs.Add(m_AbandonedCollapsedNotification);
		prefabs.Add(m_AbandonedNotification);
		prefabs.Add(m_CondemnedNotification);
		prefabs.Add(m_LevelUpNotification);
		prefabs.Add(m_TurnedOffNotification);
		prefabs.Add(m_ElectricityConnectionLane);
		prefabs.Add(m_SewageConnectionLane);
		prefabs.Add(m_WaterConnectionLane);
		prefabs.Add(m_HighRentNotification);
		prefabs.Add(m_DefaultRenterBrand);
		prefabs.Add(m_ConstructionSurface);
		prefabs.Add(m_ConstructionBorder);
		prefabs.Add(m_ConstructionObject);
		prefabs.Add(m_CollapsedObject);
		prefabs.Add(m_CollapseVFX);
		prefabs.Add(m_CollapseSFX);
		prefabs.Add(m_CollapsedSurface);
		prefabs.Add(m_FireLoopSFX);
		prefabs.Add(m_FireSpotSFX);
		prefabs.Add(m_LevelingBuildingNotificationPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<BuildingConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new BuildingConfigurationData
		{
			m_BuildingConditionIncrement = m_BuildingConditionIncrement,
			m_BuildingConditionDecrement = m_BuildingConditionDecrement,
			m_AbandonedBuildingLocalEffects = orCreateSystemManaged.GetEntity(m_AbandonedBuildingLocalEffects),
			m_AbandonedCollapsedBuildingLocalEffects = orCreateSystemManaged.GetEntity(m_AbandonedCollapsedBuildingLocalEffects),
			m_AbandonedCollapsedNotification = orCreateSystemManaged.GetEntity(m_AbandonedCollapsedNotification),
			m_AbandonedNotification = orCreateSystemManaged.GetEntity(m_AbandonedNotification),
			m_CondemnedNotification = orCreateSystemManaged.GetEntity(m_CondemnedNotification),
			m_LevelUpNotification = orCreateSystemManaged.GetEntity(m_LevelUpNotification),
			m_TurnedOffNotification = orCreateSystemManaged.GetEntity(m_TurnedOffNotification),
			m_ElectricityConnectionLane = orCreateSystemManaged.GetEntity(m_ElectricityConnectionLane),
			m_SewageConnectionLane = orCreateSystemManaged.GetEntity(m_SewageConnectionLane),
			m_WaterConnectionLane = orCreateSystemManaged.GetEntity(m_WaterConnectionLane),
			m_AbandonedDestroyDelay = m_AbandonedDestroyDelay,
			m_HighRentNotification = orCreateSystemManaged.GetEntity(m_HighRentNotification),
			m_DefaultRenterBrand = orCreateSystemManaged.GetEntity(m_DefaultRenterBrand),
			m_ConstructionSurface = orCreateSystemManaged.GetEntity(m_ConstructionSurface),
			m_ConstructionBorder = orCreateSystemManaged.GetEntity(m_ConstructionBorder),
			m_ConstructionObject = orCreateSystemManaged.GetEntity(m_ConstructionObject),
			m_CollapsedObject = orCreateSystemManaged.GetEntity(m_CollapsedObject),
			m_CollapseVFX = orCreateSystemManaged.GetEntity(m_CollapseVFX),
			m_CollapseSFX = orCreateSystemManaged.GetEntity(m_CollapseSFX),
			m_CollapseSFXDensity = m_CollapseSFXDensity,
			m_CollapsedSurface = orCreateSystemManaged.GetEntity(m_CollapsedSurface),
			m_FireLoopSFX = orCreateSystemManaged.GetEntity(m_FireLoopSFX),
			m_FireSpotSFX = orCreateSystemManaged.GetEntity(m_FireSpotSFX),
			m_LevelingBuildingNotificationPrefab = orCreateSystemManaged.GetEntity(m_LevelingBuildingNotificationPrefab)
		});
		DynamicBuffer<ZoneLevelUpResourceData> dynamicBuffer = entityManager.AddBuffer<ZoneLevelUpResourceData>(entity);
		if (m_LevelUpResources == null)
		{
			return;
		}
		for (int i = 0; i < m_LevelUpResources.Length; i++)
		{
			for (int j = 0; j < m_LevelUpResources[i].m_ResourceStack.Length; j++)
			{
				dynamicBuffer.Add(new ZoneLevelUpResourceData
				{
					m_LevelUpResource = new ResourceStack
					{
						m_Resource = EconomyUtils.GetResource(m_LevelUpResources[i].m_ResourceStack[j].m_Resource),
						m_Amount = m_LevelUpResources[i].m_ResourceStack[j].m_Amount
					},
					m_Level = m_LevelUpResources[i].m_Level
				});
			}
		}
	}
}
