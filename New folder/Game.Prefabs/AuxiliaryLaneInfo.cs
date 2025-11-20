using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class AuxiliaryLaneInfo
{
	public NetLanePrefab m_Lane;

	public float3 m_Position;

	public NetPieceRequirements[] m_RequireAll;

	public NetPieceRequirements[] m_RequireAny;

	public NetPieceRequirements[] m_RequireNone;

	public float3 m_Spacing;

	public bool m_EvenSpacing;

	public bool m_FindAnchor;
}
