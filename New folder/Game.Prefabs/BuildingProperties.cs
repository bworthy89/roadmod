using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Economy;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
public class BuildingProperties : ComponentBase
{
	[Tooltip("Having this component OVERRIDES the Apartment amount calculation which would otherwise come from Zone Prefab (Zone Properties component). Inserting value here will define the maximum amount of Apartments this building can have.")]
	public int m_ResidentialProperties;

	[Tooltip("Having this component OVERRIDES the value that would come from the Zone Prefab Zone Properties component. The listed resources can be sold by the property.")]
	public ResourceInEditor[] m_AllowedSold;

	[Tooltip("Having this component OVERRIDES the value that would come from the Zone Prefab Zone Properties component. The listed resources can be used by the property. Can be left empty.")]
	public ResourceInEditor[] m_AllowedInput;

	[Tooltip("Having this component OVERRIDES the value that would come from the Zone Prefab Zone Properties component. The listed resources can be manufactured by the property.")]
	public ResourceInEditor[] m_AllowedManufactured;

	[Tooltip("Having this component OVERRIDES the value that would come from the Zone Prefab Zone Properties component. The listed resources can be stored by the property.")]
	public ResourceInEditor[] m_AllowedStored;

	[Tooltip("Having this component OVERRIDES the value that would come from the Zone Prefab Zone Properties component. Space Multiplier represents the abstraction of amount of floors in a building. If the value on Space Multiplier is high the Apartments will be bigger. Does not affect how many Apartments are in a building. If value of Residential Properties is higher than Space Multiplier, then the zone type will be High Density, otherwise it will be Medium Density.")]
	public float m_SpaceMultiplier;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BuildingPropertyData>());
		if (EconomyUtils.GetResources(m_AllowedStored, Resource.NoResource) != Resource.NoResource)
		{
			components.Add(ComponentType.ReadWrite<WarehouseData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (!base.prefab.Has<PlaceholderBuilding>())
		{
			AddArchetypeComponents(components, GetPropertyData());
		}
	}

	public static void AddArchetypeComponents(HashSet<ComponentType> components, BuildingPropertyData propertyData)
	{
		components.Add(ComponentType.ReadWrite<Renter>());
		if (propertyData.m_ResidentialProperties > 0)
		{
			components.Add(ComponentType.ReadWrite<ResidentialProperty>());
			components.Add(ComponentType.ReadWrite<BuildingNotifications>());
			components.Add(ComponentType.ReadWrite<PropertyToBeOnMarket>());
		}
		if (propertyData.m_AllowedSold != Resource.NoResource)
		{
			components.Add(ComponentType.ReadWrite<CommercialProperty>());
			components.Add(ComponentType.ReadWrite<PropertyToBeOnMarket>());
			components.Add(ComponentType.ReadWrite<Efficiency>());
		}
		if (propertyData.m_AllowedManufactured != Resource.NoResource)
		{
			components.Add(ComponentType.ReadWrite<IndustrialProperty>());
			components.Add(ComponentType.ReadWrite<PropertyToBeOnMarket>());
			components.Add(ComponentType.ReadWrite<Efficiency>());
			if (EconomyUtils.IsExtractorResource(propertyData.m_AllowedManufactured))
			{
				components.Add(ComponentType.ReadWrite<ExtractorProperty>());
			}
			if (EconomyUtils.IsOfficeResource(propertyData.m_AllowedManufactured))
			{
				components.Add(ComponentType.ReadWrite<OfficeProperty>());
			}
		}
		if (propertyData.m_AllowedStored != Resource.NoResource)
		{
			components.Add(ComponentType.ReadWrite<IndustrialProperty>());
			components.Add(ComponentType.ReadWrite<StorageProperty>());
			components.Add(ComponentType.ReadWrite<PropertyToBeOnMarket>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, GetPropertyData());
	}

	public BuildingPropertyData GetPropertyData()
	{
		return new BuildingPropertyData
		{
			m_ResidentialProperties = m_ResidentialProperties,
			m_SpaceMultiplier = m_SpaceMultiplier,
			m_AllowedSold = EconomyUtils.GetResources(m_AllowedSold, Resource.NoResource),
			m_AllowedInput = EconomyUtils.GetResources(m_AllowedInput, EconomyUtils.GetAllResources()),
			m_AllowedManufactured = EconomyUtils.GetResources(m_AllowedManufactured, Resource.NoResource),
			m_AllowedStored = EconomyUtils.GetResources(m_AllowedStored, Resource.NoResource)
		};
	}
}
