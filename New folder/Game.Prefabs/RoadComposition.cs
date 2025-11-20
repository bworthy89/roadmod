using Unity.Entities;

namespace Game.Prefabs;

public struct RoadComposition : IComponentData, IQueryTypeParameter
{
	public Entity m_ZoneBlockPrefab;

	public float m_SpeedLimit;

	public float m_Priority;

	public RoadFlags m_Flags;
}
