using System;
using Colossal;

namespace Game.UI.Editor;

public class AssetItem : IItemPicker.Item, IComparable<AssetItem>
{
	public Hash128 guid { get; set; }

	public string fileName { get; set; }

	public int CompareTo(AssetItem other)
	{
		if (base.favorite == other.favorite)
		{
			return string.CompareOrdinal(fileName, other.fileName);
		}
		return -base.favorite.CompareTo(other.favorite);
	}
}
