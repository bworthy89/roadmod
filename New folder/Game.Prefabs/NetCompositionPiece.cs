using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetCompositionPiece : IBufferElementData
{
	public Entity m_Piece;

	public float3 m_Offset;

	public float3 m_Size;

	public NetSectionFlags m_SectionFlags;

	public NetPieceFlags m_PieceFlags;

	public int m_SectionIndex;
}
