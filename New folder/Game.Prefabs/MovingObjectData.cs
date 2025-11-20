using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct MovingObjectData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EntityArchetype m_StoppedArchetype;
}
