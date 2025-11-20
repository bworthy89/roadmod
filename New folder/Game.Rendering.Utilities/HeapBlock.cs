using System;
using System.Diagnostics;

namespace Game.Rendering.Utilities;

[DebuggerDisplay("({begin}, {end}), Length = {Length}")]
public struct HeapBlock : IComparable<HeapBlock>, IEquatable<HeapBlock>
{
	public ulong begin;

	public ulong end;

	public ulong Length => end - begin;

	public bool Empty => Length == 0;

	public HeapBlock(ulong begin, ulong end)
	{
		this.begin = begin;
		this.end = end;
	}

	public static HeapBlock OfSize(ulong begin, ulong size)
	{
		return new HeapBlock(begin, begin + size);
	}

	public int CompareTo(HeapBlock other)
	{
		return begin.CompareTo(other.begin);
	}

	public bool Equals(HeapBlock other)
	{
		return CompareTo(other) == 0;
	}
}
