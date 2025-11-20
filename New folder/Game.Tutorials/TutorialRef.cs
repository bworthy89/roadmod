using Unity.Entities;

namespace Game.Tutorials;

public struct TutorialRef : IBufferElementData
{
	public Entity m_Tutorial;

	public TutorialRef(Entity tutorial)
	{
		m_Tutorial = tutorial;
	}
}
