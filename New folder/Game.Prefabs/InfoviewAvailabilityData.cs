using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewAvailabilityData : IComponentData, IQueryTypeParameter
{
	public AreaType m_AreaType;

	public bool m_Office;
}
