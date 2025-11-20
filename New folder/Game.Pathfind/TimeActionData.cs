using Unity.Entities;

namespace Game.Pathfind;

public struct TimeActionData
{
	public Entity m_Owner;

	public PathNode m_StartNode;

	public PathNode m_EndNode;

	public PathNode m_SecondaryStartNode;

	public PathNode m_SecondaryEndNode;

	public float m_Time;

	public TimeActionFlags m_Flags;
}
