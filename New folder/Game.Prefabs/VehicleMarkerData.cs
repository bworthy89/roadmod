using Unity.Entities;

namespace Game.Prefabs;

public struct VehicleMarkerData : IComponentData, IQueryTypeParameter
{
	public VehicleType m_VehicleType;
}
