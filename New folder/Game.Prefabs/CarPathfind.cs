using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Pathfind/", new Type[] { typeof(PathfindPrefab) })]
public class CarPathfind : ComponentBase
{
	public PathfindCostInfo m_DrivingCost = new PathfindCostInfo(0f, 0f, 0.01f, 0f);

	public PathfindCostInfo m_TurningCost = new PathfindCostInfo(0f, 0f, 0f, 1f);

	public PathfindCostInfo m_UTurnCost = new PathfindCostInfo(0f, 0f, 0f, 10f);

	public PathfindCostInfo m_UnsafeUTurnCost = new PathfindCostInfo(0f, 50f, 0f, 10f);

	public PathfindCostInfo m_CurveAngleCost = new PathfindCostInfo(2f, 0f, 0f, 3f);

	public PathfindCostInfo m_LaneCrossCost = new PathfindCostInfo(0f, 0f, 0f, 2f);

	public PathfindCostInfo m_ParkingCost = new PathfindCostInfo(10f, 0f, 0f, 0f);

	public PathfindCostInfo m_SpawnCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public PathfindCostInfo m_ForbiddenCost = new PathfindCostInfo(10f, 100f, 20f, 50f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PathfindCarData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new PathfindCarData
		{
			m_DrivingCost = m_DrivingCost.ToPathfindCosts(),
			m_TurningCost = m_TurningCost.ToPathfindCosts(),
			m_UTurnCost = m_UTurnCost.ToPathfindCosts(),
			m_UnsafeUTurnCost = m_UnsafeUTurnCost.ToPathfindCosts(),
			m_CurveAngleCost = m_CurveAngleCost.ToPathfindCosts(),
			m_LaneCrossCost = m_LaneCrossCost.ToPathfindCosts(),
			m_ParkingCost = m_ParkingCost.ToPathfindCosts(),
			m_SpawnCost = m_SpawnCost.ToPathfindCosts(),
			m_ForbiddenCost = m_ForbiddenCost.ToPathfindCosts()
		});
	}
}
