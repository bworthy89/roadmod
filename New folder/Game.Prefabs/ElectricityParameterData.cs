using Colossal.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct ElectricityParameterData : IComponentData, IQueryTypeParameter
{
	public float m_InitialBatteryCharge;

	public AnimationCurve1 m_TemperatureConsumptionMultiplier;

	public float m_CloudinessSolarPenalty;

	public Entity m_ElectricityServicePrefab;

	public Entity m_ElectricityNotificationPrefab;

	public Entity m_LowVoltageNotConnectedPrefab;

	public Entity m_HighVoltageNotConnectedPrefab;

	public Entity m_BottleneckNotificationPrefab;

	public Entity m_BuildingBottleneckNotificationPrefab;

	public Entity m_NotEnoughProductionNotificationPrefab;

	public Entity m_TransformerNotificationPrefab;

	public Entity m_NotEnoughConnectedNotificationPrefab;

	public Entity m_BatteryEmptyNotificationPrefab;
}
