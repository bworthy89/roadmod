using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct TrackComposition : IComponentData, IQueryTypeParameter
{
	public TrackTypes m_TrackType;

	public float m_SpeedLimit;
}
