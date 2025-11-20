using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class FixedNetSegmentInfo
{
	public int2 m_CountRange;

	public float m_Length;

	public bool m_CanCurve;

	public NetPieceRequirements[] m_SetState;

	public NetPieceRequirements[] m_UnsetState;
}
