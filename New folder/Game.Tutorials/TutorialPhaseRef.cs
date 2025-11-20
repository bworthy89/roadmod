using Unity.Entities;

namespace Game.Tutorials;

public struct TutorialPhaseRef : IBufferElementData
{
	public Entity m_Phase;

	public TutorialPhaseRef(Entity phase)
	{
		m_Phase = phase;
	}
}
