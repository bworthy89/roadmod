using Unity.Entities;

namespace Game.Prefabs;

public struct WaterSourceData : IComponentData, IQueryTypeParameter
{
	public float m_Radius;

	public float m_height;

	public float m_InitialPolluted;
}
