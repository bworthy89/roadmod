using Colossal.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct DisasterConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_WeatherDamageNotificationPrefab;

	public Entity m_WeatherDestroyedNotificationPrefab;

	public Entity m_WaterDamageNotificationPrefab;

	public Entity m_WaterDestroyedNotificationPrefab;

	public Entity m_DestroyedNotificationPrefab;

	public float m_FloodDamageRate;

	public AnimationCurve1 m_EmergencyShelterDangerLevelExitProbability;

	public float m_InoperableEmergencyShelterExitProbability;
}
