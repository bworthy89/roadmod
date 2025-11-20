using Unity.Entities;

namespace Game.Prefabs;

public struct ResourceConsumerData : IComponentData, IQueryTypeParameter
{
	public Entity m_NoResourceNotificationPrefab;
}
