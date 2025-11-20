namespace Game.Rendering;

public struct AnimatedPropID
{
	private int m_Index;

	public static readonly AnimatedPropID None = new AnimatedPropID(-1);

	public static readonly AnimatedPropID Any = new AnimatedPropID(-2);

	public bool isValid => m_Index >= 0;

	public int index => m_Index;

	public AnimatedPropID(int index)
	{
		m_Index = index;
	}

	public static bool operator ==(AnimatedPropID a, AnimatedPropID b)
	{
		return a.m_Index == b.m_Index;
	}

	public static bool operator !=(AnimatedPropID a, AnimatedPropID b)
	{
		return a.m_Index != b.m_Index;
	}

	public override bool Equals(object obj)
	{
		if (obj is AnimatedPropID animatedPropID)
		{
			return this == animatedPropID;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Index.GetHashCode();
	}
}
