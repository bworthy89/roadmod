using Unity.Entities;

namespace Game.Prefabs;

public struct AmbientAudioSettingsData : IComponentData, IQueryTypeParameter
{
	public float m_MinHeight;

	public float m_MaxHeight;

	public float m_OverlapRatio;

	public float m_MinDistanceRatio;
}
