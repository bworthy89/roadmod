using Unity.Entities;

namespace Game.Prefabs;

public struct PathwayComposition : IComponentData, IQueryTypeParameter
{
	public float m_SpeedLimit;
}
