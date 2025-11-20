using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Annotations;
using Game.Settings;

namespace Game.UI.Editor;

public class FilePickerAdapter : ItemPicker<FileItem>.IAdapter, SearchField.IAdapter, ItemPickerFooter.IAdapter
{
	private string m_SearchQuery = string.Empty;

	private List<FileItem> m_Items;

	private List<FileItem> m_FilteredItems;

	private bool m_FilteredItemsChanged;

	[CanBeNull]
	private FileItem m_SelectedItem;

	private int m_ColumnCount;

	public Action<FileItem> EventItemSelected;

	[CanBeNull]
	public FileItem selectedItem
	{
		get
		{
			return m_SelectedItem;
		}
		set
		{
			m_SelectedItem = value;
		}
	}

	FileItem ItemPicker<FileItem>.IAdapter.selectedItem
	{
		get
		{
			return m_SelectedItem;
		}
		set
		{
			m_SelectedItem = value;
			EventItemSelected?.Invoke(m_SelectedItem);
		}
	}

	List<FileItem> ItemPicker<FileItem>.IAdapter.items => m_FilteredItems;

	public string searchQuery
	{
		get
		{
			return m_SearchQuery;
		}
		set
		{
			if (value != m_SearchQuery)
			{
				m_SearchQuery = value;
				UpdateFilteredItems();
			}
		}
	}

	int ItemPickerFooter.IAdapter.length => m_FilteredItems.Count;

	public int columnCount
	{
		get
		{
			return m_ColumnCount;
		}
		set
		{
			if (value != m_ColumnCount)
			{
				m_ColumnCount = value;
				EditorSettings editorSettings = SharedSettings.instance?.editor;
				if (editorSettings != null)
				{
					editorSettings.assetPickerColumnCount = value;
				}
			}
		}
	}

	public FilePickerAdapter(IEnumerable<FileItem> items)
	{
		m_Items = items.ToList();
		m_Items.Sort();
		m_FilteredItems = new List<FileItem>(m_Items);
		m_ColumnCount = (SharedSettings.instance?.editor)?.assetPickerColumnCount ?? 4;
	}

	public FileItem SelectItemByName(string name, StringComparison comparisonType)
	{
		m_SelectedItem = m_Items.FirstOrDefault((FileItem item) => item.path.Equals(name, comparisonType));
		return m_SelectedItem;
	}

	private void UpdateFilteredItems()
	{
		m_FilteredItems.Clear();
		List<FileItem> filteredItems = m_FilteredItems;
		IEnumerable<FileItem> collection;
		if (string.IsNullOrEmpty(m_SearchQuery))
		{
			IEnumerable<FileItem> items = m_Items;
			collection = items;
		}
		else
		{
			collection = m_Items.Where((FileItem item) => item.path.IndexOf(m_SearchQuery, StringComparison.OrdinalIgnoreCase) != -1);
		}
		filteredItems.AddRange(collection);
		m_FilteredItemsChanged = true;
	}

	bool ItemPicker<FileItem>.IAdapter.Update()
	{
		bool filteredItemsChanged = m_FilteredItemsChanged;
		m_FilteredItemsChanged = false;
		return filteredItemsChanged;
	}

	void ItemPicker<FileItem>.IAdapter.SetFavorite(int index, bool favorite)
	{
	}
}
