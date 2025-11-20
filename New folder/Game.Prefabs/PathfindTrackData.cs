using Game.Pathfind;
using Unity.Entities;

namespace Game.Prefabs;

public struct PathfindTrackData : IComponentData, IQueryTypeParameter
{
	public PathfindCosts m_DrivingCost;

	public PathfindCosts m_TwowayCost;

	public PathfindCosts m_SwitchCost;

	public PathfindCosts m_DiamondCrossingCost;

	public PathfindCosts m_CurveAngleCost;

	public PathfindCosts m_SpawnCost;
}
