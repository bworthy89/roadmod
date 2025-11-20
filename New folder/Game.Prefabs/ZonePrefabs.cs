using Game.Zones;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct ZonePrefabs
{
	private NativeArray<Entity> m_ZonePrefabs;

	public Entity this[ZoneType type] => m_ZonePrefabs[type.m_Index];

	public ZonePrefabs(NativeArray<Entity> zonePrefabs)
	{
		m_ZonePrefabs = zonePrefabs;
	}
}
