using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct AnimationMotion : IBufferElementData
{
	public float3 m_StartOffset;

	public float3 m_EndOffset;

	public quaternion m_StartRotation;

	public quaternion m_EndRotation;
}
