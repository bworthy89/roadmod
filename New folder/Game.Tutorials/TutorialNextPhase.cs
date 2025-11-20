using Unity.Entities;

namespace Game.Tutorials;

public struct TutorialNextPhase : IComponentData, IQueryTypeParameter
{
	public Entity m_NextPhase;
}
