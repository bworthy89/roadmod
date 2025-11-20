using System;

namespace Game.Rendering.Utilities;

public static class Extensions
{
	public static void Fire(this Action action)
	{
		action?.Invoke();
	}

	public static void Fire<T>(this Action<T> action, T arg1)
	{
		action?.Invoke(arg1);
	}

	public static void Fire<T, U>(this Action<T, U> action, T arg1, U arg2)
	{
		action?.Invoke(arg1, arg2);
	}
}
