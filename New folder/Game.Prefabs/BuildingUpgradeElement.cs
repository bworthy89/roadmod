using System;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct BuildingUpgradeElement : IBufferElementData, IEquatable<BuildingUpgradeElement>
{
	public Entity m_Upgrade;

	public BuildingUpgradeElement(Entity upgrade)
	{
		m_Upgrade = upgrade;
	}

	public bool Equals(BuildingUpgradeElement other)
	{
		return m_Upgrade.Equals(other.m_Upgrade);
	}

	public override int GetHashCode()
	{
		return m_Upgrade.GetHashCode();
	}
}
