using System;
using Unity.Entities;

namespace Game.Areas;

public struct AreaSearchItem : IEquatable<AreaSearchItem>
{
	public Entity m_Area;

	public int m_Triangle;

	public AreaSearchItem(Entity area, int triangle)
	{
		m_Area = area;
		m_Triangle = triangle;
	}

	public bool Equals(AreaSearchItem other)
	{
		return m_Area.Equals(other.m_Area) & m_Triangle.Equals(other.m_Triangle);
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_Area.GetHashCode()) * 31 + m_Triangle.GetHashCode();
	}
}
