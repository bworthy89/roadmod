using Unity.Entities;

namespace Game.Prefabs;

public struct BuildingMarkerData : IComponentData, IQueryTypeParameter
{
	public BuildingType m_BuildingType;
}
