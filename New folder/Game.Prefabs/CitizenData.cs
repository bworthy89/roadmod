using Unity.Entities;

namespace Game.Prefabs;

public struct CitizenData : IComponentData, IQueryTypeParameter
{
	public bool m_Male;
}
