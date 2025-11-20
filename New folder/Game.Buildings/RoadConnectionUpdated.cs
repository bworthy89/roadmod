using Unity.Entities;

namespace Game.Buildings;

public struct RoadConnectionUpdated : IComponentData, IQueryTypeParameter
{
	public Entity m_Building;

	public Entity m_Old;

	public Entity m_New;
}
