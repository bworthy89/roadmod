using Unity.Mathematics;

namespace Game.Rendering;

public struct AreaTriangleData
{
	public float3 m_APos;

	public float3 m_BPos;

	public float3 m_CPos;

	public float2 m_APrevXZ;

	public float2 m_BPrevXZ;

	public float2 m_CPrevXZ;

	public float2 m_ANextXZ;

	public float2 m_BNextXZ;

	public float2 m_CNextXZ;

	public float2 m_YMinMax;

	public float4 m_OffsetDir;

	public float m_LodDistanceFactor;
}
