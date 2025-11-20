using Unity.Entities;

namespace Game.Prefabs;

public struct LaneDeteriorationData : IComponentData, IQueryTypeParameter
{
	public float m_TrafficFactor;

	public float m_TimeFactor;
}
