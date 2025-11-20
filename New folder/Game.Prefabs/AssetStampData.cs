using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AssetStampData : IComponentData, IQueryTypeParameter
{
	public int2 m_Size;

	public uint m_UpKeepCost;
}
