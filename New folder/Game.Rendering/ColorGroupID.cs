namespace Game.Rendering;

public struct ColorGroupID
{
	private int m_Index;

	public ColorGroupID(int index)
	{
		m_Index = index;
	}

	public static bool operator ==(ColorGroupID a, ColorGroupID b)
	{
		return a.m_Index == b.m_Index;
	}

	public static bool operator !=(ColorGroupID a, ColorGroupID b)
	{
		return a.m_Index != b.m_Index;
	}

	public override bool Equals(object obj)
	{
		if (obj is ColorGroupID colorGroupID)
		{
			return this == colorGroupID;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Index.GetHashCode();
	}
}
