using Unity.Entities;

namespace Game.Prefabs;

public struct LightEffectData : IComponentData, IQueryTypeParameter
{
	public float m_Range;

	public float m_DistanceFactor;

	public float m_InvDistanceFactor;

	public int m_MinLod;

	public float m_ColorTemperature;
}
