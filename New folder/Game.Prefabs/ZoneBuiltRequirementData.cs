using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

public struct ZoneBuiltRequirementData : IComponentData, IQueryTypeParameter
{
	public Entity m_RequiredTheme;

	public Entity m_RequiredZone;

	public int m_MinimumSquares;

	public int m_MinimumCount;

	public AreaType m_RequiredType;

	public byte m_MinimumLevel;
}
