using Unity.Entities;

namespace Game.Prefabs;

public struct DistrictOptionData : IComponentData, IQueryTypeParameter
{
	public uint m_OptionMask;
}
