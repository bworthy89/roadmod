using Unity.Mathematics;

namespace Game.Rendering;

public struct AnimatedTransition2
{
	public int2 m_TransitionIndex;

	public float2 m_TransitionFrame;

	public float2 m_TransitionWeight;

	public int m_MetaIndex;

	public int m_CurrentIndex;

	public float m_CurrentFrame;
}
