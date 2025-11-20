using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Zones/", new Type[] { typeof(ZonePrefab) })]
public class ZonePollution : ComponentBase, IZoneBuildingComponent
{
	[Min(0f)]
	public float m_GroundPollution;

	[Min(0f)]
	public float m_AirPollution;

	[Min(0f)]
	public float m_NoisePollution;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ZonePollutionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new ZonePollutionData
		{
			m_GroundPollution = m_GroundPollution,
			m_AirPollution = m_AirPollution,
			m_NoisePollution = m_NoisePollution
		});
	}

	public void GetBuildingPrefabComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		components.Add(ComponentType.ReadWrite<PollutionData>());
	}

	public void GetBuildingArchetypeComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		GetBuildingPollutionData(buildingPrefab).AddArchetypeComponents(components);
	}

	public void InitializeBuilding(EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level)
	{
		if (!buildingPrefab.Has<Pollution>())
		{
			entityManager.SetComponentData(entity, GetBuildingPollutionData(buildingPrefab));
		}
	}

	private PollutionData GetBuildingPollutionData(BuildingPrefab buildingPrefab)
	{
		int lotSize = buildingPrefab.lotSize;
		return new PollutionData
		{
			m_GroundPollution = m_GroundPollution * (float)lotSize,
			m_AirPollution = m_AirPollution * (float)lotSize,
			m_NoisePollution = m_NoisePollution * (float)lotSize
		};
	}
}
