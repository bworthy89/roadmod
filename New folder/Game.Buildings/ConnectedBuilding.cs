using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[InternalBufferCapacity(0)]
public struct ConnectedBuilding : IBufferElementData, IEquatable<ConnectedBuilding>, IEmptySerializable
{
	public Entity m_Building;

	public ConnectedBuilding(Entity building)
	{
		m_Building = building;
	}

	public bool Equals(ConnectedBuilding other)
	{
		return m_Building.Equals(other.m_Building);
	}

	public override int GetHashCode()
	{
		return m_Building.GetHashCode();
	}
}
