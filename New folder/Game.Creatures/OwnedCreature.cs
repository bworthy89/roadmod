using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

[InternalBufferCapacity(0)]
public struct OwnedCreature : IBufferElementData, IEquatable<OwnedCreature>, IEmptySerializable
{
	public Entity m_Creature;

	public OwnedCreature(Entity creature)
	{
		m_Creature = creature;
	}

	public bool Equals(OwnedCreature other)
	{
		return m_Creature.Equals(other.m_Creature);
	}

	public override int GetHashCode()
	{
		return m_Creature.GetHashCode();
	}
}
