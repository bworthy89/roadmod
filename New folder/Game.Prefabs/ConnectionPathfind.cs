using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Pathfind/", new Type[] { typeof(PathfindPrefab) })]
public class ConnectionPathfind : ComponentBase
{
	public PathfindCostInfo m_BorderCost = new PathfindCostInfo(10f, 0f, 10f, 0f);

	public PathfindCostInfo m_PedestrianBorderCost = new PathfindCostInfo(10f, 0f, 10f, 0f);

	public PathfindCostInfo m_DistanceCost = new PathfindCostInfo(0.01f, 0f, 0.01f, 0f);

	public PathfindCostInfo m_AirwayCost = new PathfindCostInfo(0f, 0f, 0.02f, 0f);

	public PathfindCostInfo m_InsideCost = new PathfindCostInfo(0.01f, 0f, 0.01f, 0f);

	public PathfindCostInfo m_AreaCost = new PathfindCostInfo(0f, 0f, 0f, 0f);

	public PathfindCostInfo m_CarSpawnCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public PathfindCostInfo m_BicycleSpawnCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public PathfindCostInfo m_PedestrianSpawnCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public PathfindCostInfo m_HelicopterTakeoffCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public PathfindCostInfo m_AirplaneTakeoffCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public PathfindCostInfo m_TaxiStartCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public PathfindCostInfo m_ParkingCost = new PathfindCostInfo(10f, 0f, 0f, 0f);

	public PathfindCostInfo m_BicycleParkingCost = new PathfindCostInfo(10f, 0f, 0f, 0f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PathfindConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new PathfindConnectionData
		{
			m_BorderCost = m_BorderCost.ToPathfindCosts(),
			m_PedestrianBorderCost = m_PedestrianBorderCost.ToPathfindCosts(),
			m_DistanceCost = m_DistanceCost.ToPathfindCosts(),
			m_AirwayCost = m_AirwayCost.ToPathfindCosts(),
			m_InsideCost = m_InsideCost.ToPathfindCosts(),
			m_AreaCost = m_AreaCost.ToPathfindCosts(),
			m_CarSpawnCost = m_CarSpawnCost.ToPathfindCosts(),
			m_BicycleSpawnCost = m_BicycleSpawnCost.ToPathfindCosts(),
			m_PedestrianSpawnCost = m_PedestrianSpawnCost.ToPathfindCosts(),
			m_HelicopterTakeoffCost = m_HelicopterTakeoffCost.ToPathfindCosts(),
			m_AirplaneTakeoffCost = m_AirplaneTakeoffCost.ToPathfindCosts(),
			m_TaxiStartCost = m_TaxiStartCost.ToPathfindCosts(),
			m_ParkingCost = m_ParkingCost.ToPathfindCosts(),
			m_BicycleParkingCost = m_BicycleParkingCost.ToPathfindCosts()
		});
	}
}
