using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct ResourceConnectionData : IComponentData, IQueryTypeParameter
{
	public Resource m_Resource;

	public Entity m_ConnectionWarningNotification;
}
