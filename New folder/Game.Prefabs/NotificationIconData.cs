using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct NotificationIconData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EntityArchetype m_Archetype;
}
