using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct ConnectedRoute : IBufferElementData, IEquatable<ConnectedRoute>, IEmptySerializable
{
	public Entity m_Waypoint;

	public ConnectedRoute(Entity waypoint)
	{
		m_Waypoint = waypoint;
	}

	public bool Equals(ConnectedRoute other)
	{
		return m_Waypoint.Equals(other.m_Waypoint);
	}

	public override int GetHashCode()
	{
		return m_Waypoint.GetHashCode();
	}
}
