using System;
using System.Collections.Generic;

namespace Game.Input;

public class DefaultComparer<T> : IComparer<T> where T : struct, IComparable<T>
{
	public static DefaultComparer<T> instance = new DefaultComparer<T>();

	public int Compare(T x, T y)
	{
		return x.CompareTo(y);
	}
}
