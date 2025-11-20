using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

[InternalBufferCapacity(0)]
public struct AvailabilityElement : IBufferElementData
{
	public Entity m_Edge;

	public float2 m_Availability;
}
