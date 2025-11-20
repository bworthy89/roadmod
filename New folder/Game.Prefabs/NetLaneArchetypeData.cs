using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct NetLaneArchetypeData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EntityArchetype m_LaneArchetype;

	public EntityArchetype m_AreaLaneArchetype;

	public EntityArchetype m_EdgeLaneArchetype;

	public EntityArchetype m_EdgeSlaveArchetype;

	public EntityArchetype m_EdgeMasterArchetype;

	public EntityArchetype m_NodeLaneArchetype;

	public EntityArchetype m_NodeSlaveArchetype;

	public EntityArchetype m_NodeMasterArchetype;
}
