using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

public struct BuildingSpawnGroupData : ISharedComponentData, IQueryTypeParameter
{
	public ZoneType m_ZoneType;

	public BuildingSpawnGroupData(ZoneType type)
	{
		m_ZoneType = type;
	}
}
