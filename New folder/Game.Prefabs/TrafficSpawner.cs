using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Net;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab),
	typeof(MarkerObjectPrefab)
})]
public class TrafficSpawner : ComponentBase
{
	public RoadTypes m_RoadType = RoadTypes.Car;

	public TrackTypes m_TrackType;

	public float m_SpawnRate = 0.5f;

	public bool m_NoSlowVehicles;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TrafficSpawnerData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.TrafficSpawner>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new TrafficSpawnerData
		{
			m_SpawnRate = m_SpawnRate,
			m_RoadType = m_RoadType,
			m_TrackType = m_TrackType,
			m_NoSlowVehicles = m_NoSlowVehicles
		});
		entityManager.SetComponentData(entity, new UpdateFrameData(2));
	}
}
