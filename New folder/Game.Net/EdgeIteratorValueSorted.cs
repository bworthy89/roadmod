using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct EdgeIteratorValueSorted : IComparable<EdgeIteratorValueSorted>
{
	public Entity m_Edge;

	public uint m_SortIndex;

	public bool m_End;

	public bool m_Middle;

	public int CompareTo(EdgeIteratorValueSorted other)
	{
		return math.select(0, math.select(1, -1, m_SortIndex < other.m_SortIndex), m_SortIndex != other.m_SortIndex);
	}
}
