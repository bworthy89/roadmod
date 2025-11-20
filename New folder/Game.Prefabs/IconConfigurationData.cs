using Unity.Entities;

namespace Game.Prefabs;

public struct IconConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_SelectedMarker;

	public Entity m_FollowedMarker;
}
