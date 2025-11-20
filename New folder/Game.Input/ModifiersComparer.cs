using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Game.Input;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct ModifiersComparer : IComparer<float>
{
	public int Compare(float x, float y)
	{
		if (float.IsNaN(x))
		{
			return 1;
		}
		if (float.IsNaN(y))
		{
			return -1;
		}
		float num = Math.Abs(x);
		float num2 = Math.Abs(y);
		if (num > num2)
		{
			return -1;
		}
		if (!(num < num2))
		{
			return 0;
		}
		return 1;
	}
}
