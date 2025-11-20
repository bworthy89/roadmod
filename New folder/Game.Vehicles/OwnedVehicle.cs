using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

[InternalBufferCapacity(0)]
public struct OwnedVehicle : IBufferElementData, IEquatable<OwnedVehicle>, IEmptySerializable
{
	public Entity m_Vehicle;

	public OwnedVehicle(Entity vehicle)
	{
		m_Vehicle = vehicle;
	}

	public bool Equals(OwnedVehicle other)
	{
		return m_Vehicle.Equals(other.m_Vehicle);
	}

	public override int GetHashCode()
	{
		return m_Vehicle.GetHashCode();
	}
}
