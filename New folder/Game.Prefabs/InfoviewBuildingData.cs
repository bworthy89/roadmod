using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewBuildingData : IComponentData, IQueryTypeParameter
{
	public BuildingType m_Type;
}
