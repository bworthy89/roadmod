using Unity.Entities;

namespace Game.Prefabs;

public struct VehicleLaunchData : IComponentData, IQueryTypeParameter
{
	public TransportType m_TransportType;
}
