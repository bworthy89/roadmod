using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering;

[InternalBufferCapacity(0)]
public struct Bone : IBufferElementData, IEmptySerializable
{
	public float3 m_Position;

	public quaternion m_Rotation;

	public float3 m_Scale;
}
