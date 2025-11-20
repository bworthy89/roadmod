using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ZoneBlockData : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EntityArchetype m_Archetype;

	public MeshLayer m_AvailableLayers;

	public ushort m_AvailablePartitions;
}
