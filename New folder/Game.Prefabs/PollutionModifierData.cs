using Unity.Entities;

namespace Game.Prefabs;

public struct PollutionModifierData : IComponentData, IQueryTypeParameter, ICombineData<PollutionModifierData>
{
	public float m_GroundPollutionMultiplier;

	public float m_AirPollutionMultiplier;

	public float m_NoisePollutionMultiplier;

	public void Combine(PollutionModifierData otherData)
	{
		m_GroundPollutionMultiplier += otherData.m_GroundPollutionMultiplier;
		m_AirPollutionMultiplier += otherData.m_AirPollutionMultiplier;
		m_NoisePollutionMultiplier += otherData.m_NoisePollutionMultiplier;
	}
}
