using Unity.Entities;

namespace Game.Prefabs;

public struct CullingAudioSettingsData : IComponentData, IQueryTypeParameter
{
	public int m_FireCullMaxAmount;

	public float m_FireCullMaxDistance;

	public int m_CarEngineCullMaxAmount;

	public float m_CarEngineCullMaxDistance;

	public int m_PublicTransCullMaxAmount;

	public float m_PublicTransCullMaxDistance;
}
