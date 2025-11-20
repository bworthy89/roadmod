using Unity.Entities;

namespace Game.Prefabs;

public struct PetData : IComponentData, IQueryTypeParameter
{
	public PetType m_Type;
}
