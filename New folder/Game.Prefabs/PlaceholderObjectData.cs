using Unity.Entities;

namespace Game.Prefabs;

public struct PlaceholderObjectData : IComponentData, IQueryTypeParameter
{
	public ObjectRequirementFlags m_RequirementMask;

	public bool m_RandomizeGroupIndex;
}
