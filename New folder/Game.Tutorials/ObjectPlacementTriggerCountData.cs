using Unity.Entities;

namespace Game.Tutorials;

public struct ObjectPlacementTriggerCountData : IComponentData, IQueryTypeParameter
{
	public int m_RequiredCount;

	public int m_Count;

	public ObjectPlacementTriggerCountData(int requiredCount)
	{
		m_RequiredCount = requiredCount;
		m_Count = 0;
	}
}
