using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewBuildingStatusData : IComponentData, IQueryTypeParameter
{
	public BuildingStatusType m_Type;

	public Bounds1 m_Range;
}
