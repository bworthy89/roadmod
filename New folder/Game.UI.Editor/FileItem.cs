using System;

namespace Game.UI.Editor;

public class FileItem : IItemPicker.Item, IComparable<FileItem>
{
	public string path { get; set; }

	public int CompareTo(FileItem other)
	{
		return string.CompareOrdinal(path, other.path);
	}
}
