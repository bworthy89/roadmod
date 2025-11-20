using Unity.Entities;

namespace Game.Prefabs;

public struct UpdateFrameData : IComponentData, IQueryTypeParameter
{
	public int m_UpdateGroupIndex;

	public UpdateFrameData(int updateGroupIndex)
	{
		m_UpdateGroupIndex = updateGroupIndex;
	}
}
