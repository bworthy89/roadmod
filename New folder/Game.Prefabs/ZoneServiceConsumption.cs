using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Zones/", new Type[] { typeof(ZonePrefab) })]
public class ZoneServiceConsumption : ComponentBase, IZoneBuildingComponent
{
	public float m_Upkeep;

	public float m_ElectricityConsumption;

	public float m_WaterConsumption;

	public float m_GarbageAccumulation;

	public float m_TelecomNeed;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ZoneServiceConsumptionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new ZoneServiceConsumptionData
		{
			m_Upkeep = m_Upkeep,
			m_ElectricityConsumption = m_ElectricityConsumption,
			m_WaterConsumption = m_WaterConsumption,
			m_GarbageAccumulation = m_GarbageAccumulation,
			m_TelecomNeed = m_TelecomNeed
		});
	}

	public void GetBuildingPrefabComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		components.Add(ComponentType.ReadWrite<ConsumptionData>());
	}

	public void GetBuildingArchetypeComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		if (!buildingPrefab.Has<ServiceConsumption>())
		{
			GetBuildingConsumptionData().AddArchetypeComponents(components);
		}
	}

	public void InitializeBuilding(EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level)
	{
		if (!buildingPrefab.Has<ServiceConsumption>())
		{
			entityManager.SetComponentData(entity, GetBuildingConsumptionData());
		}
	}

	private ConsumptionData GetBuildingConsumptionData()
	{
		return new ConsumptionData
		{
			m_Upkeep = 0,
			m_ElectricityConsumption = m_ElectricityConsumption,
			m_WaterConsumption = m_WaterConsumption,
			m_GarbageAccumulation = m_GarbageAccumulation,
			m_TelecomNeed = m_TelecomNeed
		};
	}
}
