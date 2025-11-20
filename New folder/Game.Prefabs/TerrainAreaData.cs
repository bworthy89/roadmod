using Unity.Entities;

namespace Game.Prefabs;

public struct TerrainAreaData : IComponentData, IQueryTypeParameter
{
	public float m_HeightOffset;

	public float m_SlopeWidth;

	public float m_NoiseScale;

	public float m_NoiseFactor;

	public float m_AbsoluteHeight;
}
