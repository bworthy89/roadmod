using Unity.Entities;

namespace Game.Prefabs;

public struct ActivityLocationData : IComponentData, IQueryTypeParameter
{
	public ActivityMask m_ActivityMask;
}
