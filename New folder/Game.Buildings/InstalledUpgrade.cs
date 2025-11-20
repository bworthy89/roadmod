using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[InternalBufferCapacity(0)]
public struct InstalledUpgrade : IBufferElementData, IEquatable<InstalledUpgrade>, IEmptySerializable
{
	public Entity m_Upgrade;

	public uint m_OptionMask;

	public InstalledUpgrade(Entity upgrade, uint optionMask)
	{
		m_Upgrade = upgrade;
		m_OptionMask = optionMask;
	}

	public bool Equals(InstalledUpgrade other)
	{
		return m_Upgrade.Equals(other.m_Upgrade);
	}

	public override int GetHashCode()
	{
		return m_Upgrade.GetHashCode();
	}

	public static implicit operator Entity(InstalledUpgrade upgrade)
	{
		return upgrade.m_Upgrade;
	}
}
