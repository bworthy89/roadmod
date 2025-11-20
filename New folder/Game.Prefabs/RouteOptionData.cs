using Unity.Entities;

namespace Game.Prefabs;

public struct RouteOptionData : IComponentData, IQueryTypeParameter
{
	public uint m_OptionMask;
}
