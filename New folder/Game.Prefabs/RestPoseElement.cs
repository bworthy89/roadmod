using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct RestPoseElement : IBufferElementData
{
	public float3 m_Position;

	public quaternion m_Rotation;
}
