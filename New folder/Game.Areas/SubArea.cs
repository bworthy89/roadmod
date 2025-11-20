using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

[InternalBufferCapacity(0)]
public struct SubArea : IBufferElementData, IEquatable<SubArea>, IEmptySerializable
{
	public Entity m_Area;

	public SubArea(Entity area)
	{
		m_Area = area;
	}

	public bool Equals(SubArea other)
	{
		return m_Area.Equals(other.m_Area);
	}

	public override int GetHashCode()
	{
		return m_Area.GetHashCode();
	}
}
