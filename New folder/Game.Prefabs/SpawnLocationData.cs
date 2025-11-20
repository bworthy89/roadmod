using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct SpawnLocationData : IComponentData, IQueryTypeParameter
{
	public RouteConnectionType m_ConnectionType;

	public ActivityMask m_ActivityMask;

	public TrackTypes m_TrackTypes;

	public RoadTypes m_RoadTypes;

	public bool m_RequireAuthorization;

	public bool m_HangaroundOnLane;
}
