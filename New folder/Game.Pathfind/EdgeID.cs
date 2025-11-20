using System;

namespace Game.Pathfind;

public struct EdgeID : IEquatable<EdgeID>
{
	public int m_Index;

	public bool Equals(EdgeID other)
	{
		return m_Index == other.m_Index;
	}

	public override int GetHashCode()
	{
		return m_Index;
	}
}
