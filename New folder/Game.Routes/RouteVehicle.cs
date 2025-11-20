using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct RouteVehicle : IBufferElementData, IEquatable<RouteVehicle>, IEmptySerializable
{
	public Entity m_Vehicle;

	public RouteVehicle(Entity vehicle)
	{
		m_Vehicle = vehicle;
	}

	public bool Equals(RouteVehicle other)
	{
		return m_Vehicle.Equals(other.m_Vehicle);
	}

	public override int GetHashCode()
	{
		return m_Vehicle.GetHashCode();
	}
}
