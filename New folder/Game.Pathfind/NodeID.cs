using System;

namespace Game.Pathfind;

public struct NodeID : IEquatable<NodeID>
{
	public int m_Index;

	public bool Equals(NodeID other)
	{
		return m_Index == other.m_Index;
	}

	public override int GetHashCode()
	{
		return m_Index;
	}
}
