namespace Game.Pathfind;

public struct PathSpecification
{
	public PathfindCosts m_Costs;

	public EdgeFlags m_Flags;

	public PathMethod m_Methods;

	public int m_AccessRequirement;

	public float m_Length;

	public float m_MaxSpeed;

	public float m_Density;

	public RuleFlags m_Rules;

	public byte m_BlockageStart;

	public byte m_BlockageEnd;

	public byte m_FlowOffset;
}
