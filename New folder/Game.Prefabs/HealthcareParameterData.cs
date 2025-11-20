using Colossal.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct HealthcareParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_HealthcareServicePrefab;

	public Entity m_AmbulanceNotificationPrefab;

	public Entity m_HearseNotificationPrefab;

	public Entity m_FacilityFullNotificationPrefab;

	public float m_TransportWarningTime;

	public float m_NoResourceTreatmentPenalty;

	public float m_BuildingDestoryDeathRate;

	public AnimationCurve1 m_DeathRate;
}
