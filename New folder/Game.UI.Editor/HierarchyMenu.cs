using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class HierarchyMenu<T> : HierarchyMenu
{
	private SelectionType m_SelectionType;

	private List<HierarchyItem<T>> m_Items = new List<HierarchyItem<T>>();

	private List<ViewportItem<T>> m_Viewport = new List<ViewportItem<T>>();

	private int m_ViewportStartIndex;

	private int m_ViewportEndIndex;

	private bool m_ViewportDirty;

	public override string propertiesTypeName => "Game.UI.Editor.HierarchyMenu";

	public FlexLayout flex { get; set; } = FlexLayout.Fill;

	public Action onSelectionChange { get; set; }

	public SelectionType selectionType
	{
		get
		{
			return m_SelectionType;
		}
		set
		{
			if (m_SelectionType != value)
			{
				m_SelectionType = value;
				ClearSelection();
				m_ViewportDirty = true;
				SetPropertiesChanged();
			}
		}
	}

	public IEnumerable<HierarchyItem<T>> items
	{
		get
		{
			foreach (HierarchyItem<T> item in m_Items)
			{
				yield return item;
			}
		}
		set
		{
			m_Items.Clear();
			m_Items.AddRange(value);
			m_ViewportDirty = true;
			SetPropertiesChanged();
		}
	}

	public bool IsEmpty()
	{
		return m_Items.Count == 0;
	}

	public IEnumerable<T> GetSelectedItems()
	{
		int i = 0;
		while (i < m_Items.Count)
		{
			if (m_Items[i].m_Selected)
			{
				yield return m_Items[i].m_Data;
				switch (m_SelectionType)
				{
				case SelectionType.singleSelection:
					yield break;
				case SelectionType.inheritedMultiSelection:
					i += CountChildren(i);
					break;
				}
			}
			int num = i + 1;
			i = num;
		}
	}

	public bool GetSelectedItem(out T selection)
	{
		foreach (HierarchyItem<T> item in m_Items)
		{
			if (item.m_Selected)
			{
				selection = item.m_Data;
				return true;
			}
		}
		selection = default(T);
		return false;
	}

	protected override void OnSetRenderedRange(int start, int end)
	{
		m_ViewportStartIndex = start;
		m_ViewportEndIndex = end;
		m_ViewportDirty = true;
	}

	protected override void OnSetItemSelected(int viewportIndex, bool selected)
	{
		SetItemSelected(m_Viewport[viewportIndex].m_ItemIndex, selected);
	}

	public void SetItemSelected(int itemIndex, bool selected)
	{
		switch (m_SelectionType)
		{
		case SelectionType.singleSelection:
			ClearSelection();
			SetItemSelectedImpl(itemIndex, selected: true);
			break;
		case SelectionType.multiSelection:
			SetItemSelectedImpl(itemIndex, selected);
			break;
		case SelectionType.inheritedMultiSelection:
			SetItemSelectedImpl(itemIndex, selected);
			SetChildrenSelected(itemIndex, selected);
			PatchParentsSelected(itemIndex);
			break;
		}
		m_ViewportDirty = true;
		onSelectionChange?.Invoke();
	}

	protected override void OnSetItemExpanded(int viewportIndex, bool expanded)
	{
		SetItemExpanded(m_Viewport[viewportIndex].m_ItemIndex, expanded);
	}

	public void SetItemExpanded(int itemIndex, bool expanded)
	{
		HierarchyItem<T> value = m_Items[itemIndex];
		value.m_Expanded = expanded;
		m_Items[itemIndex] = value;
		m_ViewportDirty = true;
	}

	private void SetItemSelectedImpl(int itemIndex, bool selected)
	{
		HierarchyItem<T> value = m_Items[itemIndex];
		value.m_Selected = selected;
		m_Items[itemIndex] = value;
	}

	private void SetChildrenSelected(int itemIndex, bool selected)
	{
		int num = CountChildren(itemIndex);
		int num2 = itemIndex + 1;
		for (int i = num2; i < num2 + num; i++)
		{
			HierarchyItem<T> value = m_Items[i];
			value.m_Selected = selected;
			m_Items[i] = value;
		}
	}

	private void PatchParentsSelected(int itemIndex)
	{
		bool flag = true;
		int parentIndex;
		while (FindParent(itemIndex, out parentIndex))
		{
			if (flag)
			{
				int num = parentIndex + 1;
				int num2 = CountChildren(parentIndex);
				int num3;
				for (num3 = num; num3 < num + num2; num3++)
				{
					flag &= m_Items[num3].m_Selected;
					if (!flag)
					{
						break;
					}
					num3 += CountChildren(num3);
				}
			}
			SetItemSelectedImpl(parentIndex, flag);
		}
	}

	private void ClearSelection()
	{
		for (int i = 0; i < m_Items.Count; i++)
		{
			SetItemSelectedImpl(i, selected: false);
		}
	}

	private void RebuildViewport()
	{
		m_Viewport.Clear();
		int num = m_ViewportEndIndex - m_ViewportStartIndex;
		if (!FindItemIndex(m_ViewportStartIndex, out var itemIndex))
		{
			return;
		}
		for (int i = itemIndex; i < m_Items.Count; i++)
		{
			if (m_Viewport.Count > num)
			{
				break;
			}
			m_Viewport.Add(new ViewportItem<T>
			{
				m_Item = m_Items[i],
				m_ItemIndex = i
			});
			if (!m_Items[i].m_Expanded)
			{
				i += CountChildren(i);
			}
		}
	}

	private bool FindItemIndex(int visibleIndex, out int itemIndex)
	{
		int num = 0;
		for (int i = 0; i < m_Items.Count; i++)
		{
			if (num == visibleIndex)
			{
				itemIndex = i;
				return true;
			}
			num++;
			if (!m_Items[i].m_Expanded)
			{
				i += CountChildren(i);
			}
		}
		itemIndex = -1;
		return false;
	}

	private int CountChildren(int itemIndex)
	{
		int level = m_Items[itemIndex].m_Level;
		int num = itemIndex + 1;
		for (int i = num; i < m_Items.Count; i++)
		{
			if (m_Items[i].m_Level <= level)
			{
				return i - num;
			}
		}
		return m_Items.Count - num;
	}

	private bool FindParent(int itemIndex, out int parentIndex)
	{
		int level = m_Items[itemIndex].m_Level;
		for (int num = itemIndex; num >= 0; num--)
		{
			if (m_Items[num].m_Level < level)
			{
				parentIndex = num;
				return true;
			}
		}
		parentIndex = -1;
		return false;
	}

	private int CountVisibleItems()
	{
		int num = 0;
		for (int i = 0; i < m_Items.Count; i++)
		{
			num++;
			if (!m_Items[i].m_Expanded)
			{
				i += CountChildren(i);
			}
		}
		return num;
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (m_ViewportDirty)
		{
			RebuildViewport();
			m_ViewportDirty = false;
			widgetChanges |= WidgetChanges.Properties;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("viewport");
		writer.Write((IList<ViewportItem<T>>)m_Viewport);
		writer.PropertyName("singleSelection");
		writer.Write(m_SelectionType == SelectionType.singleSelection);
		writer.PropertyName("visibleCount");
		int value = CountVisibleItems();
		writer.Write(value);
		writer.PropertyName("viewportStartIndex");
		writer.Write(m_ViewportStartIndex);
		writer.PropertyName("flex");
		writer.Write(flex);
	}
}
public abstract class HierarchyMenu : Widget
{
	public enum SelectionType
	{
		singleSelection,
		multiSelection,
		inheritedMultiSelection
	}

	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget, int, int>(group, "setHierarchyRenderedRange", delegate(IWidget widget, int startVisibleIndex, int endVisibleIndex)
			{
				if (widget is HierarchyMenu hierarchyMenu)
				{
					hierarchyMenu.OnSetRenderedRange(startVisibleIndex, endVisibleIndex);
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget, int, bool>(group, "setHierarchyItemSelected", delegate(IWidget widget, int viewportIndex, bool selected)
			{
				if (widget is HierarchyMenu hierarchyMenu)
				{
					hierarchyMenu.OnSetItemSelected(viewportIndex, selected);
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget, int, bool>(group, "setHierarchyItemExpanded", delegate(IWidget widget, int viewportIndex, bool expanded)
			{
				if (widget is HierarchyMenu hierarchyMenu)
				{
					hierarchyMenu.OnSetItemExpanded(viewportIndex, expanded);
					onValueChanged(widget);
				}
			}, pathResolver);
		}
	}

	protected abstract void OnSetRenderedRange(int startVisibleIndex, int endVisibleIndex);

	protected abstract void OnSetItemSelected(int viewportIndex, bool selected);

	protected abstract void OnSetItemExpanded(int viewportIndex, bool expanded);
}
