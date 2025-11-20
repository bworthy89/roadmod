using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct LabelPosition : IBufferElementData, IEmptySerializable
{
	public Bezier4x3 m_Curve;

	public int m_ElementIndex;

	public float m_HalfLength;

	public float m_MaxScale;

	public bool m_IsUnderground;
}
