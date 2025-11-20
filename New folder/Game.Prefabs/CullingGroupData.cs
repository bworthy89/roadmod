using Unity.Entities;

namespace Game.Prefabs;

public struct CullingGroupData : IComponentData, IQueryTypeParameter
{
	public int m_GroupIndex;
}
