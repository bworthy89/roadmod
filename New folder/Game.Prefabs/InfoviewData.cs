using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewData : IComponentData, IQueryTypeParameter
{
	public uint m_NotificationMask;
}
