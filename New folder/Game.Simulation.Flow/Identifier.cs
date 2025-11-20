using System;

namespace Game.Simulation.Flow;

public struct Identifier : IEquatable<Identifier>
{
	public int m_Index;

	public int m_Version;

	public static Identifier Null => default(Identifier);

	public Identifier(int index, int version)
	{
		m_Index = index;
		m_Version = version;
	}

	public static bool operator ==(Identifier left, Identifier right)
	{
		if (left.m_Index == right.m_Index)
		{
			return left.m_Version == right.m_Version;
		}
		return false;
	}

	public static bool operator !=(Identifier left, Identifier right)
	{
		return !(left == right);
	}

	public bool Equals(Identifier other)
	{
		if (m_Index == other.m_Index)
		{
			return m_Version == other.m_Version;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Identifier other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Index;
	}
}
