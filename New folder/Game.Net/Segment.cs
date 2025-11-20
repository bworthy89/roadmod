using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Net;

public struct Segment
{
	public Bezier4x3 m_Left;

	public Bezier4x3 m_Right;

	public float2 m_Length;

	public float middleLength => math.lerp(m_Length.x, m_Length.y, 0.5f);
}
