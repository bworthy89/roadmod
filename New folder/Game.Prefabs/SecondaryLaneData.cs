using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct SecondaryLaneData : IComponentData, IQueryTypeParameter
{
	public SecondaryLaneDataFlags m_Flags;

	public float3 m_PositionOffset;

	public float2 m_LengthOffset;

	public float m_CutMargin;

	public float m_CutOffset;

	public float m_CutOverlap;

	public float m_Spacing;
}
