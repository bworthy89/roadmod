using Colossal.Mathematics;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

public struct WeatherPhenomenonData : IComponentData, IQueryTypeParameter
{
	public float m_OccurenceProbability;

	public float m_HotspotInstability;

	public float m_DamageSeverity;

	public float m_DangerLevel;

	public Bounds1 m_PhenomenonRadius;

	public Bounds1 m_HotspotRadius;

	public Bounds1 m_LightningInterval;

	public Bounds1 m_Duration;

	public Bounds1 m_OccurenceTemperature;

	public Bounds1 m_OccurenceRain;

	public Bounds1 m_OccurenceCloudiness;

	public DangerFlags m_DangerFlags;
}
