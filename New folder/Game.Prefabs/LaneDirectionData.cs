using Unity.Entities;

namespace Game.Prefabs;

public struct LaneDirectionData : IComponentData, IQueryTypeParameter
{
	public LaneDirectionType m_Left;

	public LaneDirectionType m_Forward;

	public LaneDirectionType m_Right;
}
