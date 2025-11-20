using System;
using System.Collections.Generic;
using System.Linq;
using Colossal;
using Colossal.Annotations;
using Game.Settings;

namespace Game.UI.Editor;

public class AssetPickerAdapter : ItemPicker<AssetItem>.IAdapter, SearchField.IAdapter, ItemPickerFooter.IAdapter
{
	private string m_SearchQuery = string.Empty;

	private List<AssetItem> m_Items;

	private List<AssetItem> m_FilteredItems;

	private bool m_FilteredItemsChanged;

	[CanBeNull]
	private AssetItem m_SelectedItem;

	private int m_ColumnCount;

	private bool m_UseGlobalColumnCount = true;

	private HashSet<string> m_FavoriteIds = new HashSet<string>();

	public Action<AssetItem> EventItemSelected;

	[CanBeNull]
	public AssetItem selectedItem
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

	AssetItem ItemPicker<AssetItem>.IAdapter.selectedItem
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

	List<AssetItem> ItemPicker<AssetItem>.IAdapter.items => m_FilteredItems;

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
			if (value == m_ColumnCount)
			{
				return;
			}
			m_ColumnCount = value;
			if (m_UseGlobalColumnCount)
			{
				EditorSettings editorSettings = SharedSettings.instance?.editor;
				if (editorSettings != null)
				{
					editorSettings.assetPickerColumnCount = value;
				}
			}
		}
	}

	public AssetPickerAdapter(IEnumerable<AssetItem> items, int columnCount = 0)
	{
		m_FavoriteIds.Clear();
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings.assetPickerFavorites != null)
		{
			string[] assetPickerFavorites = editorSettings.assetPickerFavorites;
			foreach (string item in assetPickerFavorites)
			{
				m_FavoriteIds.Add(item);
			}
		}
		SetItems(items);
		m_UseGlobalColumnCount = columnCount <= 0;
		if (m_UseGlobalColumnCount)
		{
			m_ColumnCount = editorSettings?.assetPickerColumnCount ?? 4;
		}
		else
		{
			m_ColumnCount = columnCount;
		}
	}

	public void SetItems(IEnumerable<AssetItem> items)
	{
		m_Items = items.ToList();
		foreach (AssetItem item in m_Items)
		{
			item.favorite = m_FavoriteIds.Contains(item.guid.ToString());
		}
		m_Items.Sort();
		m_FilteredItems = new List<AssetItem>(m_Items);
	}

	public AssetItem SelectItemByName(string name, StringComparison comparisonType)
	{
		m_SelectedItem = m_Items.FirstOrDefault((AssetItem item) => item.fileName.Equals(name, comparisonType));
		return m_SelectedItem;
	}

	public AssetItem SelectItemByGuid(Hash128 guid)
	{
		m_SelectedItem = m_Items.FirstOrDefault((AssetItem item) => item.guid == guid);
		return m_SelectedItem;
	}

	private void UpdateFilteredItems()
	{
		m_FilteredItems.Clear();
		List<AssetItem> filteredItems = m_FilteredItems;
		IEnumerable<AssetItem> collection;
		if (string.IsNullOrEmpty(m_SearchQuery))
		{
			IEnumerable<AssetItem> items = m_Items;
			collection = items;
		}
		else
		{
			collection = m_Items.Where((AssetItem item) => !string.IsNullOrEmpty(item.fileName) && item.fileName.IndexOf(m_SearchQuery, StringComparison.OrdinalIgnoreCase) != -1);
		}
		filteredItems.AddRange(collection);
		m_FilteredItemsChanged = true;
	}

	bool ItemPicker<AssetItem>.IAdapter.Update()
	{
		bool filteredItemsChanged = m_FilteredItemsChanged;
		m_FilteredItemsChanged = false;
		return filteredItemsChanged;
	}

	void ItemPicker<AssetItem>.IAdapter.SetFavorite(int index, bool favorite)
	{
		AssetItem assetItem = m_FilteredItems[index];
		string item = assetItem.guid.ToString();
		if (favorite)
		{
			m_FavoriteIds.Add(item);
		}
		else
		{
			m_FavoriteIds.Remove(item);
		}
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings != null)
		{
			editorSettings.assetPickerFavorites = m_FavoriteIds.ToArray();
		}
		assetItem.favorite = favorite;
		m_Items.Sort();
		m_FilteredItems.Sort();
		m_FilteredItemsChanged = true;
	}
}
