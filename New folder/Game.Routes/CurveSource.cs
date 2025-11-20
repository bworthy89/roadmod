using Unity.Entities;
using Unity.Mathematics;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct CurveSource : IBufferElementData
{
	public Entity m_Entity;

	public float2 m_Range;

	public float2 m_Offset;
}
