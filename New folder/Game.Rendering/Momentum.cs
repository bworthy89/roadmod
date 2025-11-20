using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(0)]
public struct Momentum : IBufferElementData, IEmptySerializable
{
	public float m_Momentum;
}
