using System;

namespace Game.Prefabs;

[Serializable]
public class NetSubSectionInfo
{
	public NetSectionPrefab m_Section;

	public NetPieceRequirements[] m_RequireAll;

	public NetPieceRequirements[] m_RequireAny;

	public NetPieceRequirements[] m_RequireNone;
}
