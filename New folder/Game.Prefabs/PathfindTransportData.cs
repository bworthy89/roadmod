using Game.Pathfind;
using Unity.Entities;

namespace Game.Prefabs;

public struct PathfindTransportData : IComponentData, IQueryTypeParameter
{
	public PathfindCosts m_OrderingCost;

	public PathfindCosts m_StartingCost;

	public PathfindCosts m_TravelCost;
}
