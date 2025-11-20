using Unity.Entities;

namespace Game.Prefabs;

public struct CityOptionData : IComponentData, IQueryTypeParameter
{
	public uint m_OptionMask;
}
