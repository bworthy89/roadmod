using Unity.Entities;

namespace Game.Prefabs;

public struct SpawnableObjectData : IComponentData, IQueryTypeParameter
{
	public Entity m_RandomizationGroup;

	public int m_Probability;
}
