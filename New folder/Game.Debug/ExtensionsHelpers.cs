using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Game.Debug;

public static class ExtensionsHelpers
{
	public static void Add<T>(this ObservableList<T> list, IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			list.Add(item);
		}
	}

	public static int Remove<T>(this ObservableList<T> list, IEnumerable<T> items)
	{
		if (items == null)
		{
			return 0;
		}
		int num = 0;
		foreach (T item in items)
		{
			num += (list.Remove(item) ? 1 : 0);
		}
		return num;
	}
}
