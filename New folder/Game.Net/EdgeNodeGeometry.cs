using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Net;

public struct EdgeNodeGeometry
{
	public Segment m_Left;

	public Segment m_Right;

	public Bezier4x3 m_Middle;

	public Bounds3 m_Bounds;

	public float4 m_SyncVertexTargetsLeft;

	public float4 m_SyncVertexTargetsRight;

	public float m_MiddleRadius;
}
