using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[InternalBufferCapacity(0)]
public struct SpawnLocationElement : IBufferElementData, IEquatable<SpawnLocationElement>, IEmptySerializable
{
	public Entity m_SpawnLocation;

	public SpawnLocationType m_Type;

	public SpawnLocationElement(Entity spawnLocation, SpawnLocationType type)
	{
		m_SpawnLocation = spawnLocation;
		m_Type = type;
	}

	public bool Equals(SpawnLocationElement other)
	{
		return m_SpawnLocation.Equals(other.m_SpawnLocation);
	}

	public override int GetHashCode()
	{
		return m_SpawnLocation.GetHashCode();
	}
}
