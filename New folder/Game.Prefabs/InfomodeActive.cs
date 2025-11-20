using Unity.Entities;

namespace Game.Prefabs;

public struct InfomodeActive : IComponentData, IQueryTypeParameter
{
	public int m_Priority;

	public int m_Index;

	public int m_SecondaryIndex;

	public InfomodeActive(int priority, int index, int secondaryIndex)
	{
		m_Priority = priority;
		m_Index = index;
		m_SecondaryIndex = secondaryIndex;
	}
}
