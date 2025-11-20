using Unity.Entities;

namespace Game.Prefabs;

public struct ZonePollutionData : IComponentData, IQueryTypeParameter
{
	public float m_GroundPollution;

	public float m_AirPollution;

	public float m_NoisePollution;
}
