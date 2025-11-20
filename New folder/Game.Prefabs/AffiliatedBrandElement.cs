using System;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct AffiliatedBrandElement : IBufferElementData, IComparable<AffiliatedBrandElement>
{
	public Entity m_Brand;

	public int CompareTo(AffiliatedBrandElement other)
	{
		return m_Brand.Index - other.m_Brand.Index;
	}
}
