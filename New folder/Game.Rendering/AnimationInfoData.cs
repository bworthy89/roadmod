using Unity.Mathematics;

namespace Game.Rendering;

public struct AnimationInfoData
{
	public int m_Offset;

	public int m_Hierarchy;

	public int m_Shapes;

	public int m_Bones;

	public int m_InverseBones;

	public int m_ShapeCount;

	public int m_BoneCount;

	public int m_Type;

	public float3 m_PositionMin;

	public float3 m_PositionRange;
}
