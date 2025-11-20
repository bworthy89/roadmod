using Unity.Entities;

namespace Game.Prefabs;

public struct PedestrianLaneData : IComponentData, IQueryTypeParameter
{
	public Entity m_NotWalkLanePrefab;

	public ActivityMask m_ActivityMask;
}
