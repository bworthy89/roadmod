using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Creatures;

public struct GroupCreature : IBufferElementData, IEmptySerializable
{
	public Entity m_Creature;

	public GroupCreature(Entity creature)
	{
		m_Creature = creature;
	}
}
