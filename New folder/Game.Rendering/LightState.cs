using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(0)]
public struct LightState : IBufferElementData, IEmptySerializable
{
	public float m_Intensity;

	public float m_Color;
}
