using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct NetPieceData : IComponentData, IQueryTypeParameter
{
	public Bounds1 m_HeightRange;

	public float4 m_SurfaceHeights;

	public float m_Width;

	public float m_Length;

	public float m_WidthOffset;

	public float m_NodeOffset;

	public float m_SideConnectionOffset;
}
