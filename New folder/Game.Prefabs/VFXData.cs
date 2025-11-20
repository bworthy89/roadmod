using Unity.Entities;

namespace Game.Prefabs;

public struct VFXData : IComponentData, IQueryTypeParameter
{
	public int m_MaxCount;

	public int m_Index;
}
