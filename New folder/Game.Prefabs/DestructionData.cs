using Unity.Entities;

namespace Game.Prefabs;

public struct DestructionData : IComponentData, IQueryTypeParameter
{
	public EventTargetType m_RandomTargetType;

	public float m_OccurenceProbability;
}
