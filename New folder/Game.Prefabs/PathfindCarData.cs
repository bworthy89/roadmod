using Game.Pathfind;
using Unity.Entities;

namespace Game.Prefabs;

public struct PathfindCarData : IComponentData, IQueryTypeParameter
{
	public PathfindCosts m_DrivingCost;

	public PathfindCosts m_TurningCost;

	public PathfindCosts m_UnsafeTurningCost;

	public PathfindCosts m_UTurnCost;

	public PathfindCosts m_UnsafeUTurnCost;

	public PathfindCosts m_CurveAngleCost;

	public PathfindCosts m_LaneCrossCost;

	public PathfindCosts m_ParkingCost;

	public PathfindCosts m_SpawnCost;

	public PathfindCosts m_ForbiddenCost;
}
