using System;
using Unity.Entities;

namespace Game.Prefabs;

public struct ZoneBuiltDataKey : IEquatable<ZoneBuiltDataKey>
{
	public Entity m_Zone;

	public int m_Level;

	public bool Equals(ZoneBuiltDataKey other)
	{
		if (m_Zone.Equals(other.m_Zone))
		{
			return m_Level.Equals(other.m_Level);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_Zone.GetHashCode()) * 31 + m_Level.GetHashCode();
	}
}
