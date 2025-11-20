using System;

namespace Game.Prefabs;

[Serializable]
public class NetEdgeStateInfo
{
	public NetPieceRequirements[] m_RequireAll;

	public NetPieceRequirements[] m_RequireAny;

	public NetPieceRequirements[] m_RequireNone;

	public NetPieceRequirements[] m_SetState;
}
