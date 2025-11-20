using Unity.Entities;

namespace Game.Prefabs;

public struct UnlockFilterData : IComponentData, IQueryTypeParameter
{
	public int m_UnlockUniqueID;
}
