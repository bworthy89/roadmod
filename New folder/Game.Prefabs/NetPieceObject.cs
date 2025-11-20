using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetPieceObject : IBufferElementData
{
	public Entity m_Prefab;

	public float3 m_Position;

	public float3 m_Offset;

	public float3 m_Spacing;

	public float2 m_UseCurveRotation;

	public float m_MinLength;

	public int m_Probability;

	public float2 m_CurveOffsetRange;

	public quaternion m_Rotation;

	public CompositionFlags m_CompositionAll;

	public CompositionFlags m_CompositionAny;

	public CompositionFlags m_CompositionNone;

	public NetSectionFlags m_SectionAll;

	public NetSectionFlags m_SectionAny;

	public NetSectionFlags m_SectionNone;

	public SubObjectFlags m_Flags;
}
