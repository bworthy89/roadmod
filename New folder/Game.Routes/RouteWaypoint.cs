using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct RouteWaypoint : IBufferElementData, IEmptySerializable
{
	public Entity m_Waypoint;

	public RouteWaypoint(Entity waypoint)
	{
		m_Waypoint = waypoint;
	}
}
