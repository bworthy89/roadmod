using System;
using System.IO;
using Colossal.Annotations;

namespace Game.UI.Editor;

public class Item : IItemPicker.Item, IComparable<Item>
{
	[CanBeNull]
	public string parentDir { get; set; }

	public string name { get; set; }

	public Type type { get; set; }

	public string fullName { get; set; }

	[CanBeNull]
	public string relativePath
	{
		get
		{
			if (parentDir != null)
			{
				return Path.Combine(parentDir, name);
			}
			return name;
		}
	}

	public int CompareTo(Item other)
	{
		if (base.favorite == other.favorite)
		{
			return string.CompareOrdinal(name, other.name);
		}
		return -base.favorite.CompareTo(other.favorite);
	}
}
