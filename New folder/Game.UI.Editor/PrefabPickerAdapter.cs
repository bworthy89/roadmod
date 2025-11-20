using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Annotations;
using Colossal.PSI.Common;
using Game.Prefabs;
using Game.Settings;
using Game.UI.Localization;

namespace Game.UI.Editor;

public class PrefabPickerAdapter : ItemPicker<PrefabItem>.IAdapter, PopupSearchField.IAdapter, SearchField.IAdapter, ItemPickerFooter.IAdapter, FilterMenu.IAdapter
{
	public const int kMaxHistoryLength = 100;

	public const int kMaxHistoryDisplayLength = 20;

	private ImageSystem m_ImageSystem;

	private string m_SearchQuery = string.Empty;

	private bool m_SearchQueryIsFavorite;

	private bool m_SearchQueryChanged;

	private List<PrefabItem> m_Items = new List<PrefabItem>();

	private bool m_ItemsChanged;

	private List<string> m_AvailableFilters = new List<string>();

	private List<string> m_ActiveFilters = new List<string>();

	private bool m_ActiveFiltersChanged;

	private List<PrefabItem> m_FilteredItems = new List<PrefabItem>();

	private bool m_FilteredItemsChanged;

	private PrefabItem m_SelectedItem;

	private PrefabBase m_SelectedPrefab;

	private List<string> m_SearchHistory = new List<string>();

	private HashSet<string> m_SearchFavorites = new HashSet<string>();

	private List<PopupSearchField.Suggestion> m_SearchSuggestions = new List<PopupSearchField.Suggestion>();

	private HashSet<string> m_FavoriteIds = new HashSet<string>();

	private int m_ColumnCount;

	public Action<PrefabBase> EventPrefabSelected;

	public bool displayPrefabTypeTooltip { get; set; }

	[CanBeNull]
	public PrefabBase selectedPrefab
	{
		get
		{
			return m_SelectedPrefab;
		}
		set
		{
			m_SelectedPrefab = value;
		}
	}

	public List<string> availableFilters => m_AvailableFilters;

	public List<string> activeFilters => m_ActiveFilters;

	public Action onAvailableFiltersChanged { get; set; }

	PrefabItem ItemPicker<PrefabItem>.IAdapter.selectedItem
	{
		get
		{
			return m_SelectedItem;
		}
		set
		{
			m_SelectedItem = value;
			m_SelectedPrefab = value?.prefab;
			EventPrefabSelected?.Invoke(m_SelectedPrefab);
			if (!string.IsNullOrEmpty(m_SearchQuery.Trim()) && m_FilteredItems.Contains(m_SelectedItem))
			{
				m_SearchHistory.Remove(m_SearchQuery);
				m_SearchHistory.Insert(0, m_SearchQuery);
				if (m_SearchHistory.Count > 100)
				{
					m_SearchHistory.RemoveRange(100, m_SearchHistory.Count - 100);
				}
				EditorSettings editorSettings = SharedSettings.instance?.editor;
				if (editorSettings != null)
				{
					editorSettings.prefabPickerSearchHistory = m_SearchHistory.ToArray();
				}
			}
		}
	}

	List<PrefabItem> ItemPicker<PrefabItem>.IAdapter.items => m_FilteredItems;

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
				m_SearchQueryIsFavorite = m_SearchFavorites.Contains(value);
				m_SearchQueryChanged = true;
			}
		}
	}

	public bool searchQueryIsFavorite => m_SearchQueryIsFavorite;

	IEnumerable<PopupSearchField.Suggestion> PopupSearchField.IAdapter.searchSuggestions => m_SearchSuggestions;

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
					editorSettings.prefabPickerColumnCount = value;
				}
			}
		}
	}

	public void LoadSettings()
	{
		m_SearchHistory.Clear();
		m_SearchFavorites.Clear();
		m_ColumnCount = 1;
		m_FavoriteIds.Clear();
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings == null)
		{
			return;
		}
		if (editorSettings.prefabPickerSearchHistory != null)
		{
			m_SearchHistory.AddRange(editorSettings.prefabPickerSearchHistory);
		}
		if (editorSettings.prefabPickerSearchFavorites != null)
		{
			string[] prefabPickerSearchFavorites = editorSettings.prefabPickerSearchFavorites;
			foreach (string item in prefabPickerSearchFavorites)
			{
				m_SearchFavorites.Add(item);
			}
		}
		m_ColumnCount = editorSettings.prefabPickerColumnCount;
		if (editorSettings.prefabPickerFavorites != null)
		{
			string[] prefabPickerSearchFavorites = editorSettings.prefabPickerFavorites;
			foreach (string item2 in prefabPickerSearchFavorites)
			{
				m_FavoriteIds.Add(item2);
			}
		}
	}

	public void SetPrefabs([ItemCanBeNull] ICollection<PrefabBase> prefabs)
	{
		m_Items.Clear();
		m_Items.Capacity = prefabs.Count;
		m_ItemsChanged = true;
		m_SelectedItem = null;
		m_AvailableFilters.Clear();
		m_ActiveFilters.Clear();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (PrefabBase prefab in prefabs)
		{
			string prefabID = EditorPrefabUtils.GetPrefabID(prefab);
			PrefabItem prefabItem = new PrefabItem
			{
				prefab = prefab,
				displayName = EditorPrefabUtils.GetPrefabLabel(prefab)
			};
			if (prefab != null && displayPrefabTypeTooltip)
			{
				prefabItem.tooltip = LocalizedString.Value(prefab.GetType().Name);
			}
			if (prefab != null)
			{
				prefabItem.tags.AddRange(EditorPrefabUtils.GetPrefabTags(prefab.GetType()));
				foreach (ComponentBase component in prefab.components)
				{
					prefabItem.tags.Add(component.GetType().Name.ToLowerInvariant());
				}
				foreach (string tag in prefabItem.tags)
				{
					hashSet.Add(tag);
				}
				prefabItem.image = ImageSystem.GetThumbnail(prefab);
				if (TryGetDLCBadge(prefab, out var icon))
				{
					prefabItem.badge = icon;
				}
			}
			if (prefabID != null)
			{
				prefabItem.favorite = m_FavoriteIds.Contains(prefabID);
			}
			m_Items.Add(prefabItem);
		}
		m_AvailableFilters.AddRange(hashSet);
		m_AvailableFilters.Sort();
		onAvailableFiltersChanged?.Invoke();
		m_Items.Sort();
	}

	private bool TryGetDLCBadge(PrefabBase prefab, out string icon)
	{
		if (prefab.TryGet<AssetPackItem>(out var component) && component.m_Packs != null)
		{
			AssetPackPrefab[] packs = component.m_Packs;
			for (int i = 0; i < packs.Length; i++)
			{
				if (packs[i].TryGet<UIObject>(out var component2) && !string.IsNullOrEmpty(component2.m_Icon))
				{
					icon = component2.m_Icon;
					return true;
				}
			}
		}
		if (prefab.TryGet<ContentPrerequisite>(out var component3))
		{
			ContentPrefab contentPrerequisite = component3.m_ContentPrerequisite;
			if (contentPrerequisite.TryGet<UIObject>(out var component4) && !string.IsNullOrEmpty(component4.m_Icon))
			{
				icon = component4.m_Icon;
				return true;
			}
			if (contentPrerequisite.TryGet<DlcRequirement>(out var component5))
			{
				string dlcName = PlatformManager.instance.GetDlcName(component5.m_Dlc);
				if (!string.IsNullOrEmpty(dlcName))
				{
					icon = "Media/DLC/" + dlcName + ".svg";
					return true;
				}
			}
		}
		icon = null;
		return false;
	}

	public PrefabBase SelectPrefabByName(string name, StringComparison comparisonType)
	{
		m_SelectedPrefab = m_Items.Select((PrefabItem item) => item.prefab).FirstOrDefault((PrefabBase prefab) => prefab.name.Equals(name, comparisonType));
		return m_SelectedPrefab;
	}

	public void Update()
	{
		if (m_SelectedPrefab != m_SelectedItem?.prefab)
		{
			m_SelectedItem = m_Items.FirstOrDefault((PrefabItem item) => item.prefab == m_SelectedPrefab);
		}
		if (m_ItemsChanged || m_SearchQueryChanged || m_ActiveFiltersChanged)
		{
			m_ItemsChanged = false;
			m_SearchQueryChanged = false;
			m_ActiveFiltersChanged = false;
			UpdateFilteredItems();
		}
	}

	private void UpdateFilteredItems()
	{
		m_SearchSuggestions.Clear();
		m_FilteredItems.Clear();
		m_FilteredItemsChanged = true;
		string[] array = m_SearchQuery.Split(' ');
		string[] words = array.Where((string p) => p.Length > 0 && !p.StartsWith("#")).ToArray();
		string[] tags = (from p in array.Take(array.Length - 1)
			where p.Length > 1 && p.StartsWith("#")
			select p.Substring(1)).Concat(m_ActiveFilters).ToArray();
		string incompleteTag = GetIncompleteTag(array);
		if (words.Length != 0 || tags.Length != 0 || incompleteTag != null)
		{
			m_SearchSuggestions.AddRange(m_SearchHistory.Where((string s) => !m_SearchFavorites.Contains(s) && s.StartsWith(m_SearchQuery, StringComparison.OrdinalIgnoreCase)).Take(20).Select(PopupSearchField.Suggestion.NonFavorite));
			m_SearchSuggestions.AddRange(m_SearchFavorites.Where((string s) => s.StartsWith(m_SearchQuery, StringComparison.OrdinalIgnoreCase)).Select(PopupSearchField.Suggestion.Favorite));
			m_FilteredItems.AddRange(m_Items.Where(delegate(PrefabItem item)
			{
				if (item.prefab == null)
				{
					return false;
				}
				bool num = words.Length == 0 || words.All((string word) => item.prefab.name.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1);
				string typeName = item.prefab.GetType().Name;
				bool flag = words.Any((string word) => word.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) != -1);
				bool flag2 = tags.Length == 0 || tags.Any((string tag) => item.tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
				bool flag3 = incompleteTag == null || item.tags.Any((string tag) => tag.StartsWith(incompleteTag, StringComparison.OrdinalIgnoreCase));
				return ((num || flag) && flag2 && flag3) ? true : false;
			}));
		}
		else
		{
			m_SearchSuggestions.AddRange(m_SearchHistory.Where((string s) => !m_SearchFavorites.Contains(s)).Take(20).Select(PopupSearchField.Suggestion.NonFavorite));
			m_SearchSuggestions.AddRange(m_SearchFavorites.Select(PopupSearchField.Suggestion.Favorite));
			m_FilteredItems.AddRange(m_Items);
		}
		m_SearchSuggestions.Sort();
	}

	[CanBeNull]
	private static string GetIncompleteTag(string[] searchParts)
	{
		if (searchParts.Length == 0)
		{
			return null;
		}
		string text = searchParts[^1];
		if (text.Length <= 1 || !text.StartsWith("#"))
		{
			return null;
		}
		return text.Substring(1);
	}

	bool ItemPicker<PrefabItem>.IAdapter.Update()
	{
		bool filteredItemsChanged = m_FilteredItemsChanged;
		m_FilteredItemsChanged = false;
		return filteredItemsChanged;
	}

	void ItemPicker<PrefabItem>.IAdapter.SetFavorite(int index, bool favorite)
	{
		PrefabItem prefabItem = m_FilteredItems[index];
		string prefabID = EditorPrefabUtils.GetPrefabID(prefabItem.prefab);
		if (prefabID != null)
		{
			if (favorite)
			{
				m_FavoriteIds.Add(prefabID);
			}
			else
			{
				m_FavoriteIds.Remove(prefabID);
			}
			EditorSettings editorSettings = SharedSettings.instance?.editor;
			if (editorSettings != null)
			{
				editorSettings.prefabPickerFavorites = m_FavoriteIds.ToArray();
			}
			prefabItem.favorite = favorite;
			m_Items.Sort();
			m_FilteredItems.Sort();
			m_FilteredItemsChanged = true;
		}
	}

	void PopupSearchField.IAdapter.SetFavorite(string query, bool favorite)
	{
		if (favorite)
		{
			m_SearchFavorites.Add(query);
		}
		else
		{
			m_SearchFavorites.Remove(query);
		}
		if (query == m_SearchQuery)
		{
			m_SearchQueryIsFavorite = favorite;
		}
		EditorSettings editorSettings = SharedSettings.instance?.editor;
		if (editorSettings != null)
		{
			editorSettings.prefabPickerSearchFavorites = m_SearchFavorites.ToArray();
			editorSettings.prefabPickerSearchHistory = m_SearchHistory.ToArray();
		}
		UpdateFilteredItems();
	}

	public void ToggleFilter(string filter, bool active)
	{
		if (active)
		{
			if (!m_ActiveFilters.Contains(filter))
			{
				m_ActiveFilters.Add(filter);
			}
		}
		else
		{
			m_ActiveFilters.Remove(filter);
		}
		m_ActiveFiltersChanged = true;
	}

	public void ClearFilters()
	{
		m_ActiveFilters.Clear();
		m_ActiveFiltersChanged = true;
	}
}
