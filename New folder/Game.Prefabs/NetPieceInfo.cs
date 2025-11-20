using System;
using Unity.Mathematics;

namespace Game.Prefabs;

[Serializable]
public class NetPieceInfo
{
	public NetPiecePrefab m_Piece;

	public NetPieceRequirements[] m_RequireAll;

	public NetPieceRequirements[] m_RequireAny;

	public NetPieceRequirements[] m_RequireNone;

	public float3 m_Offset;
}
