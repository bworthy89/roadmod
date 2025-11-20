using Unity.Entities;

namespace Game.City;

public struct MilestoneReachedEvent : IComponentData, IQueryTypeParameter
{
	public Entity m_Milestone;

	public int m_Index;

	public MilestoneReachedEvent(Entity milestone, int index)
	{
		m_Milestone = milestone;
		m_Index = index;
	}
}
