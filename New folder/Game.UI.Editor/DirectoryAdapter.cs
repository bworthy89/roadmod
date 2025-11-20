using System;
using System.Collections.Generic;
using System.Linq;
using Game.Settings;

namespace Game.UI.Editor;

public class DirectoryAdapter : ItemPicker<Item>.IAdapter, SearchField.IAdapter
{
	private DirectoryPanelBase m_Panel;

	private Item m_SelectedItem;

	private List<Item> m_Items;

	private bool m_Dirty;

	private HashSet<string> m_FavoriteIds = new HashSet<string>();

	public string searchQuery = string.Empty;

	private string m_SearchQuery = string.Empty;

	public string directoryPath { get; set; }

	public Item selectedItem
	{
		get
		{
			return m_SelectedItem;
		}
		set
		{
			if (value != m_SelectedItem)
			{
				m_SelectedItem = value;
				m_Panel.OnSelect(value);
			}
		}
	}

	public List<Item> items
	{
		get
		{
			return m_Items.Where(delegate(Item item)
			{
				bool num = (string.IsNullOrEmpty(searchQuery) ? (item.parentDir == directoryPath) : (directoryPath == null || (item.fullName != null && item.fullName.StartsWith(directoryPath) && item.name + "/" != directoryPath)));
				bool flag = item.name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) != -1;
				return num && flag;
			}).ToList();
		}
		set
		{
			if (value == m_Items)
			{
				return;
			}
			m_Items = value;
			m_FavoriteIds.Clear();
			EditorSettings editorSettings = SharedSettings.instance?.editor;
			if (editorSettings?.directoryPickerFavorites != null)
			{
				m_FavoriteIds.UnionWith(editorSettings.directoryPickerFavorites);
			}
			foreach (Item item in m_Items)
			{
				item.favorite = m_FavoriteIds.Contains(item.relativePath);
			}
			m_Items.Sort();
			m_Dirty = true;
		}
	}

	int ItemPicker<Item>.IAdapter.columnCount => 1;

	string SearchField.IAdapter.searchQuery
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
			}
		}
	}

	public DirectoryAdapter(DirectoryPanelBase panel)
	{
		m_Panel = panel;
	}

	bool ItemPicker<Item>.IAdapter.Update()
	{
		bool dirty = m_Dirty;
		m_Dirty = false;
		return dirty;
	}

	void ItemPicker<Item>.IAdapter.SetFavorite(int index, bool favorite)
	{
		Item item = items[index];
		string relativePath = item.relativePath;
		if (favorite)
		{
			m_FavoriteIds.Add(relativePath);
		}
		else
		{
			m_FavoriteIds.Remove(relativePath);
		}
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings != null)
		{
			editorSettings.directoryPickerFavorites = m_FavoriteIds.ToArray();
		}
		item.favorite = favorite;
		m_Items.Sort();
		items = m_Items;
		m_Dirty = true;
	}
}
