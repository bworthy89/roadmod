using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering;

[InternalBufferCapacity(0)]
public struct BoneHistory : IBufferElementData, IEmptySerializable
{
	public float4x4 m_Matrix;
}
