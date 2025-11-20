using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetSectionPiece : IBufferElementData
{
	public Entity m_Piece;

	public CompositionFlags m_CompositionAll;

	public CompositionFlags m_CompositionAny;

	public CompositionFlags m_CompositionNone;

	public NetSectionFlags m_SectionAll;

	public NetSectionFlags m_SectionAny;

	public NetSectionFlags m_SectionNone;

	public NetPieceFlags m_Flags;

	public float3 m_Offset;
}
