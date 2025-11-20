using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering;

[InternalBufferCapacity(0)]
public struct FadeBatch : IBufferElementData
{
	public Entity m_Source;

	public float3 m_Velocity;
}
