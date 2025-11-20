using Unity.Entities;

namespace Game.Pathfind;

public struct CreateActionData
{
	public Entity m_Owner;

	public PathNode m_StartNode;

	public PathNode m_MiddleNode;

	public PathNode m_EndNode;

	public PathNode m_SecondaryStartNode;

	public PathNode m_SecondaryEndNode;

	public PathSpecification m_Specification;

	public PathSpecification m_SecondarySpecification;

	public LocationSpecification m_Location;
}
