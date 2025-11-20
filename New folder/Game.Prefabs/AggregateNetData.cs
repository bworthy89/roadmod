using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct AggregateNetData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EntityArchetype m_Archetype;
}
