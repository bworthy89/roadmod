using Unity.Entities;

namespace Game.Prefabs;

public struct TutorialActivationEventData : IBufferElementData
{
	public Entity m_Tutorial;
}
