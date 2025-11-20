namespace Game.Prefabs;

public struct AnimationLayerMask
{
	public uint m_Mask;

	public AnimationLayerMask(AnimationLayer layer)
	{
		if (layer == AnimationLayer.None)
		{
			m_Mask = 0u;
		}
		else
		{
			m_Mask = (uint)(1 << (int)(layer - 1));
		}
	}
}
