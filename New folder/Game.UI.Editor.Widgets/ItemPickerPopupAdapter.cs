using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.UI.Editor.Widgets;

public class ItemPickerPopupAdapter<T> : ItemPicker<ItemPickerPopup<T>.Item>.IAdapter, PopupSearchField.IAdapter, SearchField.IAdapter, ItemPickerFooter.IAdapter where T : IEquatable<T>
{
	public Action<T> onSelectedItemChanged;

	private List<ItemPickerPopup<T>.Item> m_Items = new List<ItemPickerPopup<T>.Item>();

	private ItemPickerPopup<T>.Item m_SelectedItem;

	private string m_SearchQuery = string.Empty;

	private bool m_FilteredItemsDirty;

	public ItemPickerPopup<T>.Item selectedItem
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
				onSelectedItemChanged?.Invoke((m_SelectedItem != null) ? m_SelectedItem.m_Value : default(T));
			}
		}
	}

	public T selectedValue
	{
		get
		{
			if (m_SelectedItem == null)
			{
				return default(T);
			}
			return m_SelectedItem.m_Value;
		}
		set
		{
			if (m_SelectedItem == null || !object.Equals(m_SelectedItem.m_Value, value))
			{
				m_SelectedItem = m_Items.Find((ItemPickerPopup<T>.Item item) => object.Equals(item.m_Value, value));
			}
		}
	}

	public List<ItemPickerPopup<T>.Item> items => filteredItems;

	public List<ItemPickerPopup<T>.Item> filteredItems { get; } = new List<ItemPickerPopup<T>.Item>();

	public int length => items.Count;

	public int columnCount { get; set; } = 1;

	public string searchQuery
	{
		get
		{
			return m_SearchQuery;
		}
		set
		{
			m_SearchQuery = value;
			m_FilteredItemsDirty = true;
		}
	}

	public IEnumerable<PopupSearchField.Suggestion> searchSuggestions => Enumerable.Empty<PopupSearchField.Suggestion>();

	public bool searchQueryIsFavorite => false;

	public bool Update()
	{
		if (m_FilteredItemsDirty)
		{
			UpdateFilteredItems();
			return true;
		}
		return false;
	}

	public void SetItems(IEnumerable<ItemPickerPopup<T>.Item> newItems)
	{
		m_Items.Clear();
		m_Items.AddRange(newItems);
		m_FilteredItemsDirty = true;
		if (m_SelectedItem != null && !m_Items.Contains(m_SelectedItem))
		{
			m_SelectedItem = null;
		}
	}

	private void UpdateFilteredItems()
	{
		m_FilteredItemsDirty = false;
		filteredItems.Clear();
		string[] searchParts = (from word in m_SearchQuery.Split(' ')
			where word.Length > 0
			select word).ToArray();
		List<ItemPickerPopup<T>.Item> list = filteredItems;
		IEnumerable<ItemPickerPopup<T>.Item> collection;
		if (searchParts.Length == 0)
		{
			IEnumerable<ItemPickerPopup<T>.Item> enumerable = m_Items;
			collection = enumerable;
		}
		else
		{
			collection = m_Items.Where((ItemPickerPopup<T>.Item item) => item.m_SearchTerms != null && item.m_SearchTerms.Length != 0 && item.m_SearchTerms.Any((string term) => searchParts.All((string word) => term.Contains(word, StringComparison.OrdinalIgnoreCase))));
		}
		list.AddRange(collection);
	}

	public void SetFavorite(int index, bool favorite)
	{
	}

	public void SetFavorite(string query, bool favorite)
	{
	}
}
