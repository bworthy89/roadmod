using Unity.Entities;

namespace Game.Tutorials;

public struct TutorialData : IComponentData, IQueryTypeParameter
{
	public int m_Priority;

	public TutorialData(int priority)
	{
		m_Priority = priority;
	}
}
