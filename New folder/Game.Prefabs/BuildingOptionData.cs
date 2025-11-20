using Unity.Entities;

namespace Game.Prefabs;

public struct BuildingOptionData : IComponentData, IQueryTypeParameter
{
	public uint m_OptionMask;
}
