using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct AreaData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EntityArchetype m_Archetype;
}
