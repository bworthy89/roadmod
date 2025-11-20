using System;
using System.Collections.Generic;
using Game.Economy;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Zones/", new Type[] { typeof(ZonePrefab) })]
public class ZoneProperties : ComponentBase, IZoneBuildingComponent
{
	[Tooltip("If set TRUE the Zone Buildings will scale the amount of Residential Properties, based on Lot Size and the Building Level. If set to FALSE then this Zone Type will be LOW DENSITY\nAmount of Apartments is calculated in the following formula:\n = (1f + 0.25f * (level - 1)) * buildingPrefab.lotSize * m_ResidentialProperties)")]
	public bool m_ScaleResidentials;

	[Tooltip("If SCALE RESIDENTIALS is set to TRUE, then this Zone type can be MEDIUM or HIGH DENSITY. Value here determines how many Apartments there are per cell of a building. This is then multiplied with Lot Size to get the total amount of Apartments in the building.\nIf SCALE RESIDENTIALS is set to FALSE, then this Zone type is LOW DENSITY. Amount of Apartments in the buildings are set by the value defined here. No other factor will be taken into consideration or will affect the amount of Apartments.")]
	public float m_ResidentialProperties;

	[Tooltip("Space Multiplier represents the abstraction of amount of floors in a building. If the value on Space Multiplier is high the Apartments will be bigger. Does not affect how many Apartments are in a building. If value of Residential Properties is higher than Space Multiplier, then the zone type will be High Density, otherwise it will be Medium Density.")]
	public float m_SpaceMultiplier = 1f;

	[Tooltip("The listed resources can be sold by the zone property.")]
	public ResourceInEditor[] m_AllowedSold;

	[Tooltip("The listed resources can be used by the zone property. Can be left empty.")]
	public ResourceInEditor[] m_AllowedInput;

	[Tooltip("The listed resources can be manufactured by the zone property.")]
	public ResourceInEditor[] m_AllowedManufactured;

	[Tooltip("The listed resources can be stored by the zone property.")]
	public ResourceInEditor[] m_AllowedStored;

	public float m_FireHazardMultiplier = 1f;

	[Tooltip("The rent price of this zone type won't be affected by land value, for example low rent residential building")]
	public bool m_IgnoreLandValue;

	public LevelUpResource[] m_LevelUpResources;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ZonePropertiesData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new ZonePropertiesData
		{
			m_ScaleResidentials = m_ScaleResidentials,
			m_ResidentialProperties = m_ResidentialProperties,
			m_SpaceMultiplier = m_SpaceMultiplier,
			m_FireHazardMultiplier = m_FireHazardMultiplier,
			m_IgnoreLandValue = m_IgnoreLandValue,
			m_AllowedSold = EconomyUtils.GetResources(m_AllowedSold, Resource.NoResource),
			m_AllowedManufactured = EconomyUtils.GetResources(m_AllowedManufactured, Resource.NoResource),
			m_AllowedStored = EconomyUtils.GetResources(m_AllowedStored, Resource.NoResource)
		});
		if (m_LevelUpResources == null || m_LevelUpResources.Length == 0)
		{
			return;
		}
		DynamicBuffer<ZoneLevelUpResourceData> dynamicBuffer = entityManager.AddBuffer<ZoneLevelUpResourceData>(entity);
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

	public void GetBuildingPrefabComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		components.Add(ComponentType.ReadWrite<BuildingPropertyData>());
	}

	public void InitializeBuilding(EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level)
	{
		if (!buildingPrefab.Has<BuildingProperties>())
		{
			BuildingPropertyData buildingPropertyData = GetBuildingPropertyData(buildingPrefab, level);
			entityManager.SetComponentData(entity, buildingPropertyData);
		}
	}

	public void GetBuildingArchetypeComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		if (!buildingPrefab.Has<BuildingProperties>())
		{
			BuildingPropertyData buildingPropertyData = GetBuildingPropertyData(buildingPrefab, level);
			BuildingProperties.AddArchetypeComponents(components, buildingPropertyData);
		}
	}

	private BuildingPropertyData GetBuildingPropertyData(BuildingPrefab buildingPrefab, byte level)
	{
		float num = (m_ScaleResidentials ? ((1f + 0.25f * (float)(level - 1)) * (float)buildingPrefab.lotSize) : 1f);
		return new BuildingPropertyData
		{
			m_ResidentialProperties = (int)math.round(num * m_ResidentialProperties),
			m_AllowedSold = EconomyUtils.GetResources(m_AllowedSold, Resource.NoResource),
			m_AllowedInput = EconomyUtils.GetResources(m_AllowedInput, EconomyUtils.GetAllResources()),
			m_AllowedManufactured = EconomyUtils.GetResources(m_AllowedManufactured, Resource.NoResource),
			m_AllowedStored = EconomyUtils.GetResources(m_AllowedStored, Resource.NoResource),
			m_SpaceMultiplier = m_SpaceMultiplier
		};
	}
}
