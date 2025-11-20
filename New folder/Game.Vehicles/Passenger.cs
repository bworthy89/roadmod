using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

[InternalBufferCapacity(0)]
public struct Passenger : IBufferElementData, IEquatable<Passenger>, IEmptySerializable
{
	public Entity m_Passenger;

	public Passenger(Entity passenger)
	{
		m_Passenger = passenger;
	}

	public bool Equals(Passenger other)
	{
		return m_Passenger.Equals(other.m_Passenger);
	}

	public override int GetHashCode()
	{
		return m_Passenger.GetHashCode();
	}
}
