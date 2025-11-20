using Game.Pathfind;
using Unity.Entities;

namespace Game.Prefabs;

public struct PathfindPedestrianData : IComponentData, IQueryTypeParameter
{
	public PathfindCosts m_WalkingCost;

	public PathfindCosts m_CrosswalkCost;

	public PathfindCosts m_UnsafeCrosswalkCost;

	public PathfindCosts m_SpawnCost;
}
