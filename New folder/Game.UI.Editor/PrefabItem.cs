using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Prefabs;

namespace Game.UI.Editor;

public class PrefabItem : IItemPicker.Item, IComparable<PrefabItem>
{
	[CanBeNull]
	public PrefabBase prefab { get; set; }

	public List<string> tags { get; set; } = new List<string>();

	public int CompareTo(PrefabItem other)
	{
		if (base.favorite == other.favorite)
		{
			return string.CompareOrdinal(prefab?.name, other.prefab?.name);
		}
		return -base.favorite.CompareTo(other.favorite);
	}
}
