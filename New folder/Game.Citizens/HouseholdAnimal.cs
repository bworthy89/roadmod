using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

[InternalBufferCapacity(1)]
public struct HouseholdAnimal : IBufferElementData, IEquatable<HouseholdAnimal>, IEmptySerializable
{
	public Entity m_HouseholdPet;

	public HouseholdAnimal(Entity householdPet)
	{
		m_HouseholdPet = householdPet;
	}

	public bool Equals(HouseholdAnimal other)
	{
		return m_HouseholdPet.Equals(other.m_HouseholdPet);
	}

	public override int GetHashCode()
	{
		return m_HouseholdPet.GetHashCode();
	}
}
