using Unity.Entities;

namespace Game.Prefabs;

public struct PolicyData : IComponentData, IQueryTypeParameter
{
	public int m_Visibility;
}
