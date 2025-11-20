using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewTransportStopData : IComponentData, IQueryTypeParameter
{
	public TransportType m_Type;
}
