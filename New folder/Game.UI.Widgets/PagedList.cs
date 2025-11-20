#define UNITY_ASSERTIONS
using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Unity.Assertions;
using UnityEngine;

namespace Game.UI.Widgets;

public class PagedList : NamedWidgetWithTooltip, IExpandable, IPaged, IListWidget, IWidget, IJsonWritable, IContainerWidget, IDisableCallback
{
	private int m_Length;

	private int m_CurrentPageIndex;

	private int m_ChildStartIndex;

	private int m_ChildEndIndex;

	private List<IWidget> m_Children = new List<IWidget>();

	private bool m_Expanded;

	public bool expanded
	{
		get
		{
			return m_Expanded;
		}
		set
		{
			m_Expanded = value;
			SetPropertiesChanged();
			SetChildrenChanged();
		}
	}

	public IListAdapter adapter { get; set; }

	public int level { get; set; }

	public int pageSize { get; set; } = 10;

	public int pageCount => (m_Length + pageSize - 1) / pageSize;

	public IList<IWidget> children => m_Children;

	public int currentPageIndex
	{
		get
		{
			return m_CurrentPageIndex;
		}
		set
		{
			m_CurrentPageIndex = Mathf.Clamp(value, 0, pageCount - 1);
			CalculateChildIndices(m_CurrentPageIndex, m_Length, out m_ChildStartIndex, out m_ChildEndIndex);
			SetPropertiesChanged();
		}
	}

	public override IList<IWidget> visibleChildren
	{
		get
		{
			if (!expanded)
			{
				return Array.Empty<IWidget>();
			}
			return m_Children;
		}
	}

	public int AddElement()
	{
		int num = adapter.AddElement();
		ShowElement(num);
		return num;
	}

	public void InsertElement(int index)
	{
		adapter.InsertElement(index);
	}

	public int DuplicateElement(int index)
	{
		int num = adapter.DuplicateElement(index);
		ShowElement(num);
		return num;
	}

	public void MoveElement(int fromIndex, int toIndex)
	{
		adapter.MoveElement(fromIndex, toIndex);
	}

	public void DeleteElement(int index)
	{
		adapter.DeleteElement(index);
	}

	public void Clear()
	{
		adapter.Clear();
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		int length = adapter.length;
		if (length != m_Length)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Length = length;
		}
		CalculateIndices(m_ChildStartIndex, m_Length, out var pageIndex, out var childStartIndex, out var childEndIndex);
		if (pageIndex != m_CurrentPageIndex || childStartIndex != m_ChildStartIndex || childEndIndex != m_ChildEndIndex)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_CurrentPageIndex = pageIndex;
			m_ChildStartIndex = childStartIndex;
			m_ChildEndIndex = childEndIndex;
		}
		if (adapter.UpdateRange(m_ChildStartIndex, m_ChildEndIndex))
		{
			if (m_Expanded)
			{
				widgetChanges |= WidgetChanges.Children;
			}
			m_Children.Clear();
			m_Children.AddRange(adapter.BuildElementsInRange());
			Assert.AreEqual(m_ChildEndIndex - m_ChildStartIndex, m_Children.Count);
			if (m_Disabled)
			{
				foreach (IWidget child in m_Children)
				{
					DisableChildren(child);
				}
			}
		}
		return widgetChanges;
	}

	private void DisableChildren(IWidget child)
	{
		if (child is IDisableCallback disableCallback)
		{
			disableCallback.disabled = () => true;
		}
		if (!(child is IContainerWidget containerWidget))
		{
			return;
		}
		foreach (IWidget child2 in containerWidget.children)
		{
			DisableChildren(child2);
		}
	}

	private void ShowElement(int elementIndex)
	{
		if (elementIndex != -1)
		{
			CalculateIndices(elementIndex, adapter.length, out m_CurrentPageIndex, out m_ChildStartIndex, out m_ChildEndIndex);
		}
	}

	private void CalculateIndices(int elementIndex, int length, out int pageIndex, out int childStartIndex, out int childEndIndex)
	{
		pageIndex = Mathf.Min(elementIndex / pageSize, Math.Max(0, (length + pageSize - 1) / pageSize - 1));
		CalculateChildIndices(pageIndex, length, out childStartIndex, out childEndIndex);
	}

	private void CalculateChildIndices(int pageIndex, int length, out int childStartIndex, out int childEndIndex)
	{
		childStartIndex = pageIndex * pageSize;
		childEndIndex = Mathf.Min((pageIndex + 1) * pageSize, length);
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("expanded");
		writer.Write(expanded);
		writer.PropertyName("resizable");
		writer.Write(adapter.resizable);
		writer.PropertyName("sortable");
		writer.Write(adapter.sortable);
		writer.PropertyName("length");
		writer.Write(m_Length);
		writer.PropertyName("currentPageIndex");
		writer.Write(currentPageIndex);
		writer.PropertyName("pageCount");
		writer.Write(pageCount);
		writer.PropertyName("childStartIndex");
		writer.Write(m_ChildStartIndex);
		writer.PropertyName("childEndIndex");
		writer.Write(m_ChildEndIndex);
	}
}
