using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class NetSectionInfo
{
	public NetSectionPrefab m_Section;

	public NetPieceRequirements[] m_RequireAll;

	public NetPieceRequirements[] m_RequireAny;

	public NetPieceRequirements[] m_RequireNone;

	public NetPieceLayerMask m_HiddenLayers;

	public bool m_Invert;

	public bool m_Flip;

	public bool m_Median;

	public bool m_HalfLength;

	public float3 m_Offset;
}
