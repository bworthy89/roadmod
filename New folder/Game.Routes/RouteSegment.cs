using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct RouteSegment : IBufferElementData, IEmptySerializable
{
	public Entity m_Segment;

	public RouteSegment(Entity segment)
	{
		m_Segment = segment;
	}
}
