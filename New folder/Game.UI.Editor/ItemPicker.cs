using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Widgets;
using Unity.Mathematics;

namespace Game.UI.Editor;

public class ItemPicker<T> : Widget, IItemPicker where T : IItemPicker.Item
{
	public interface IAdapter
	{
		[CanBeNull]
		T selectedItem { get; set; }

		List<T> items { get; }

		int columnCount { get; }

		bool Update();

		void SetFavorite(int index, bool favorite);
	}

	private int m_Length;

	private int m_SelectedIndex = -1;

	private int m_ColumnCount = 1;

	private int m_StartIndex;

	private List<T> m_VisibleItems = new List<T>();

	public IAdapter adapter { get; set; }

	public FlexLayout flex { get; set; } = FlexLayout.Fill;

	public int selectedIndex
	{
		get
		{
			return m_SelectedIndex;
		}
		set
		{
			adapter.selectedItem = adapter.items[value];
		}
	}

	public bool hasImages { get; set; } = true;

	public bool hasFavorites { get; set; }

	public int visibleStartIndex { get; set; }

	public int visibleEndIndex { get; set; }

	public bool selectOnFocus { get; set; }

	public override string propertiesTypeName => "Game.UI.Editor.ItemPicker";

	public void SetFavorite(int index, bool favorite)
	{
		adapter.SetFavorite(index, favorite);
		SetPropertiesChanged();
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		bool num = adapter.Update();
		int count = adapter.items.Count;
		if (count != m_Length)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Length = count;
		}
		if (adapter.selectedItem != null)
		{
			if (m_SelectedIndex == -1 || m_SelectedIndex >= adapter.items.Count || adapter.items[m_SelectedIndex] != adapter.selectedItem)
			{
				widgetChanges |= WidgetChanges.Properties;
				m_SelectedIndex = adapter.items.IndexOf(adapter.selectedItem);
			}
		}
		else
		{
			if (m_SelectedIndex != -1)
			{
				widgetChanges |= WidgetChanges.Properties;
			}
			m_SelectedIndex = -1;
		}
		if (adapter.columnCount != m_ColumnCount)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_ColumnCount = adapter.columnCount;
		}
		visibleStartIndex = math.clamp(visibleStartIndex, 0, m_Length);
		visibleEndIndex = math.clamp(visibleEndIndex, visibleStartIndex, m_Length);
		int num2 = visibleEndIndex - visibleStartIndex;
		if (num || visibleStartIndex != m_StartIndex || num2 != m_VisibleItems.Count)
		{
			widgetChanges |= WidgetChanges.Properties | WidgetChanges.Children;
			m_StartIndex = visibleStartIndex;
			m_VisibleItems.Clear();
			for (int i = visibleStartIndex; i < visibleEndIndex; i++)
			{
				m_VisibleItems.Add(adapter.items[i]);
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("flex");
		writer.Write(flex);
		writer.PropertyName("selectedIndex");
		writer.Write(selectedIndex);
		writer.PropertyName("hasImages");
		writer.Write(hasImages);
		writer.PropertyName("hasFavorites");
		writer.Write(hasFavorites);
		writer.PropertyName("columnCount");
		writer.Write(m_ColumnCount);
		writer.PropertyName("length");
		writer.Write(m_Length);
		writer.PropertyName("visibleStartIndex");
		writer.Write(m_StartIndex);
		writer.PropertyName("visibleItems");
		writer.ArrayBegin(m_VisibleItems.Count);
		foreach (T visibleItem in m_VisibleItems)
		{
			writer.Write(visibleItem);
		}
		writer.ArrayEnd();
		writer.PropertyName("selectOnFocus");
		writer.Write(selectOnFocus);
	}
}
