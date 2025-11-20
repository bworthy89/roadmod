using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct TrafficLightData : IComponentData, IQueryTypeParameter
{
	public TrafficLightType m_Type;

	public Bounds1 m_ReachOffset;
}
