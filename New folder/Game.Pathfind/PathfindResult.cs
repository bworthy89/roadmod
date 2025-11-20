using Unity.Entities;

namespace Game.Pathfind;

public struct PathfindResult
{
	public Entity m_Origin;

	public Entity m_Destination;

	public float m_Distance;

	public float m_Duration;

	public float m_TotalCost;

	public PathMethod m_Methods;

	public int m_GraphTraversal;

	public int m_PathLength;

	public ErrorCode m_ErrorCode;
}
