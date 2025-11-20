using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct ArrowPosition : IBufferElementData, IEmptySerializable
{
	public float3 m_Position;

	public float3 m_Direction;

	public float m_MaxScale;

	public bool m_IsTrack;

	public bool m_IsUnderground;
}
