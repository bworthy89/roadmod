using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct CurveElement : IBufferElementData, IEmptySerializable
{
	public Bezier4x3 m_Curve;
}
