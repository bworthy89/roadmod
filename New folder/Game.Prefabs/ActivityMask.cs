namespace Game.Prefabs;

public struct ActivityMask
{
	public uint m_Mask;

	public ActivityMask(ActivityType type)
	{
		if (type == ActivityType.None)
		{
			m_Mask = 0u;
		}
		else
		{
			m_Mask = (uint)(1 << (int)(type - 1));
		}
	}
}
