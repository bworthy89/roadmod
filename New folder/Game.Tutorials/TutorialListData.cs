using Unity.Entities;

namespace Game.Tutorials;

public struct TutorialListData : IComponentData, IQueryTypeParameter
{
	public int m_Priority;

	public TutorialListData(int priority)
	{
		m_Priority = priority;
	}
}
