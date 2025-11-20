using Unity.Entities;

namespace Game.Prefabs;

public struct CreatureData : IComponentData, IQueryTypeParameter
{
	public ActivityMask m_SupportedActivities;

	public GenderMask m_Gender;
}
