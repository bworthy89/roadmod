using Unity.Entities;

namespace Game.Prefabs;

public struct RenderedAreaData : IComponentData, IQueryTypeParameter
{
	public float m_HeightOffset;

	public float m_ExpandAmount;

	public float m_LodBias;

	public int m_BatchIndex;
}
