using Unity.Entities;

namespace Game.Prefabs;

public struct EnclosedAreaData : IComponentData, IQueryTypeParameter
{
	public Entity m_BorderLanePrefab;

	public bool m_CounterClockWise;
}
