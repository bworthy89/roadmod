using Colossal.Collections;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering;

[InternalBufferCapacity(1)]
public struct Animated : IBufferElementData, IEmptySerializable
{
	public NativeHeapBlock m_BoneAllocation;

	public int m_MetaIndex;

	public float4 m_Time;

	public float2 m_MovementSpeed;

	public float m_Interpolation;

	public short m_ClipIndexBody0;

	public short m_ClipIndexBody0I;

	public short m_ClipIndexBody1;

	public short m_ClipIndexBody1I;

	public short m_ClipIndexFace0;

	public short m_ClipIndexFace1;
}
