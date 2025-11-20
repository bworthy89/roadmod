using Unity.Entities;

namespace Game.Pathfind;

public struct Edge
{
	public Entity m_Owner;

	public NodeID m_StartID;

	public NodeID m_MiddleID;

	public NodeID m_EndID;

	public float m_StartCurvePos;

	public float m_EndCurvePos;

	public PathSpecification m_Specification;

	public LocationSpecification m_Location;
}
