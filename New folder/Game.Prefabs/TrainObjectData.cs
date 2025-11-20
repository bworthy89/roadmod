using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct TrainObjectData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EntityArchetype m_ControllerArchetype;

	public EntityArchetype m_StoppedControllerArchetype;
}
