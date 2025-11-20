using Unity.Entities;

namespace Game.Tutorials;

public struct TutorialTrigger : IComponentData, IQueryTypeParameter
{
	public Entity m_Trigger;
}
