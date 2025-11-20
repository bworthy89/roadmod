using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct NetCompositionData : IComponentData, IQueryTypeParameter
{
	public float4 m_SyncVertexOffsetsLeft;

	public float4 m_SyncVertexOffsetsRight;

	public Bounds1 m_HeightRange;

	public Bounds1 m_SurfaceHeight;

	public float4 m_EdgeHeights;

	public float2 m_SideConnectionOffset;

	public CompositionFlags m_Flags;

	public CompositionState m_State;

	public float m_Width;

	public float m_MiddleOffset;

	public float m_WidthOffset;

	public float m_NodeOffset;

	public int m_MinLod;
}
