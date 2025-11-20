using Game.Areas;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct QuantityObjectData : IComponentData, IQueryTypeParameter
{
	public Resource m_Resources;

	public MapFeature m_MapFeature;

	public uint m_StepMask;
}
