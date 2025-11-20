using Unity.Entities;

namespace Game.Prefabs;

public struct HouseholdPetData : IComponentData, IQueryTypeParameter
{
	public PetType m_Type;
}
