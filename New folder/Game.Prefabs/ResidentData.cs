using Unity.Entities;

namespace Game.Prefabs;

public struct ResidentData : IComponentData, IQueryTypeParameter
{
	public AgeMask m_Age;
}
