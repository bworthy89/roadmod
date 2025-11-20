using Unity.Entities;

namespace Game.Prefabs;

public struct TransportStopMarkerData : IComponentData, IQueryTypeParameter
{
	public TransportType m_TransportType;

	public bool m_StopTypeA;

	public bool m_StopTypeB;
}
