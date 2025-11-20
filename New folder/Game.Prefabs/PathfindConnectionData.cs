using Game.Pathfind;
using Unity.Entities;

namespace Game.Prefabs;

public struct PathfindConnectionData : IComponentData, IQueryTypeParameter
{
	public PathfindCosts m_BorderCost;

	public PathfindCosts m_PedestrianBorderCost;

	public PathfindCosts m_DistanceCost;

	public PathfindCosts m_AirwayCost;

	public PathfindCosts m_InsideCost;

	public PathfindCosts m_AreaCost;

	public PathfindCosts m_CarSpawnCost;

	public PathfindCosts m_BicycleSpawnCost;

	public PathfindCosts m_PedestrianSpawnCost;

	public PathfindCosts m_HelicopterTakeoffCost;

	public PathfindCosts m_AirplaneTakeoffCost;

	public PathfindCosts m_TaxiStartCost;

	public PathfindCosts m_ParkingCost;

	public PathfindCosts m_BicycleParkingCost;
}
