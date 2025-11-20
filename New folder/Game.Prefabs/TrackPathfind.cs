using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Pathfind/", new Type[] { typeof(PathfindPrefab) })]
public class TrackPathfind : ComponentBase
{
	public PathfindCostInfo m_DrivingCost = new PathfindCostInfo(0f, 0f, 0.01f, 0f);

	public PathfindCostInfo m_TwowayCost = new PathfindCostInfo(0f, 0f, 0f, 5f);

	public PathfindCostInfo m_SwitchCost = new PathfindCostInfo(0f, 0f, 0f, 2f);

	public PathfindCostInfo m_DiamondCrossingCost = new PathfindCostInfo(0f, 0f, 0f, 2f);

	public PathfindCostInfo m_CurveAngleCost = new PathfindCostInfo(2f, 0f, 0f, 3f);

	public PathfindCostInfo m_SpawnCost = new PathfindCostInfo(5f, 0f, 0f, 0f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PathfindTrackData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new PathfindTrackData
		{
			m_DrivingCost = m_DrivingCost.ToPathfindCosts(),
			m_TwowayCost = m_TwowayCost.ToPathfindCosts(),
			m_SwitchCost = m_SwitchCost.ToPathfindCosts(),
			m_DiamondCrossingCost = m_DiamondCrossingCost.ToPathfindCosts(),
			m_CurveAngleCost = m_CurveAngleCost.ToPathfindCosts(),
			m_SpawnCost = m_SpawnCost.ToPathfindCosts()
		});
	}
}
