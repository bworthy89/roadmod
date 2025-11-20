using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct ProceduralLight : IBufferElementData
{
	public float4 m_Color;

	public float4 m_Color2;

	public EmissiveProperties.Purpose m_Purpose;

	public float m_ResponseSpeed;

	public int m_AnimationIndex;
}
