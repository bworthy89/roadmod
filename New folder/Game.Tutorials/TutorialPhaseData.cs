using Unity.Entities;

namespace Game.Tutorials;

public struct TutorialPhaseData : IComponentData, IQueryTypeParameter
{
	public TutorialPhaseType m_Type;

	public float m_OverrideCompletionDelay;
}
