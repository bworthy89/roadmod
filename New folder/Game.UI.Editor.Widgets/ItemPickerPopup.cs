using System;
using System.Collections.Generic;
using Game.Reflection;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor.Widgets;

public class ItemPickerPopup<T> : IValueFieldPopup<T> where T : IEquatable<T>
{
	public class Item : IItemPicker.Item
	{
		public T m_Value;

		public string[] m_SearchTerms;
	}

	private static readonly LocalizedString kNoneValue = LocalizedString.Id("Editor.NONE_VALUE");

	private ITypedValueAccessor<T> m_Accessor;

	private ItemPickerPopupAdapter<T> m_Adapter;

	public IList<IWidget> children { get; }

	public ItemPickerPopup(bool hasFooter = true, bool hasImages = true)
	{
		m_Adapter = new ItemPickerPopupAdapter<T>();
		ItemPickerPopupAdapter<T> adapter = m_Adapter;
		adapter.onSelectedItemChanged = (Action<T>)Delegate.Combine(adapter.onSelectedItemChanged, new Action<T>(OnSelectedItemChanged));
		List<IWidget> list = new List<IWidget>
		{
			new PopupSearchField
			{
				adapter = m_Adapter,
				hasFavorites = false
			},
			new ItemPicker<Item>
			{
				adapter = m_Adapter,
				hasFavorites = false,
				hasImages = hasImages
			}
		};
		if (hasFooter)
		{
			list.Add(new ItemPickerFooter
			{
				adapter = m_Adapter
			});
		}
		children = list;
		ContainerExtensions.SetDefaults(children);
	}

	public void SetItems(IEnumerable<Item> items)
	{
		m_Adapter.SetItems(items);
	}

	public void Attach(ITypedValueAccessor<T> accessor)
	{
		m_Accessor = accessor;
	}

	public void Detach()
	{
		m_Adapter.searchQuery = string.Empty;
	}

	private void OnSelectedItemChanged(T value)
	{
		m_Accessor.SetTypedValue(value);
	}

	public bool Update()
	{
		m_Adapter.selectedValue = m_Accessor.GetTypedValue();
		return false;
	}

	public LocalizedString GetDisplayValue(T value)
	{
		if (m_Adapter.selectedItem != null)
		{
			return m_Adapter.selectedItem.displayName;
		}
		return kNoneValue;
	}
}
