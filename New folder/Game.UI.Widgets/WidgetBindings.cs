#define UNITY_ASSERTIONS
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Unity.Assertions;

namespace Game.UI.Widgets;

public class WidgetBindings : CompositeBinding, IReader<IWidget>
{
	private string m_Group;

	private List<IWidget> m_LastChildren = new List<IWidget>();

	private List<int> m_CurrentPath = new List<int>();

	private RawValueBinding m_ChildrenBinding;

	public IList<IWidget> children { get; set; } = new List<IWidget>();

	public bool active => m_ChildrenBinding.active;

	public event ValueChangedCallback EventValueChanged;

	public WidgetBindings(string group, string name = "children")
	{
		m_Group = group;
		AddBinding(m_ChildrenBinding = new RawValueBinding(group, name, WriteChildren));
	}

	public void AddDefaultBindings()
	{
		AddBindings<InvokableBindings>();
		AddBindings<SettableBindings>();
		AddBindings<ExpandableBindings>();
		AddBindings<ListBindings>();
		AddBindings<PagedBindings>();
	}

	public void AddBindings<U>() where U : IWidgetBindingFactory, new()
	{
		AddBindings(new U());
	}

	public void AddBindings(IWidgetBindingFactory bindingFactory)
	{
		foreach (IBinding item in bindingFactory.CreateBindings(m_Group, this, OnValueChanged))
		{
			AddBinding(item);
		}
	}

	private void OnValueChanged(IWidget widget)
	{
		UpdateChildrenBinding();
		this.EventValueChanged?.Invoke(widget);
	}

	public override bool Update()
	{
		UpdateChildrenBinding();
		return base.Update();
	}

	private void UpdateChildrenBinding()
	{
		if (active)
		{
			bool flag = !children.SequenceEqual(m_LastChildren);
			if (flag)
			{
				m_LastChildren.Clear();
				m_LastChildren.AddRange(children);
				ContainerExtensions.SetDefaults(m_LastChildren);
			}
			m_CurrentPath.Clear();
			flag |= UpdateSubTree(children, !flag);
			Assert.AreEqual(0, m_CurrentPath.Count);
			if (flag)
			{
				m_ChildrenBinding.Update();
			}
		}
	}

	private bool UpdateSubTree(IList<IWidget> widgets, bool patch)
	{
		bool result = false;
		for (int i = 0; i < widgets.Count; i++)
		{
			IWidget widget = widgets[i];
			WidgetChanges widgetChanges = widget.Update();
			m_CurrentPath.Add(i);
			if (UpdateSubTree(widget.visibleChildren, patch && (widgetChanges & WidgetChanges.Children) == 0))
			{
				widgetChanges |= WidgetChanges.Children;
			}
			if (widgetChanges != WidgetChanges.None)
			{
				if (patch)
				{
					Widget.PatchWidget(m_ChildrenBinding, m_CurrentPath, widget, widgetChanges);
				}
				else
				{
					result = true;
				}
			}
			m_CurrentPath.RemoveAt(m_CurrentPath.Count - 1);
		}
		return result;
	}

	private void WriteChildren(IJsonWriter writer)
	{
		writer.Write((IList<IWidget>)m_LastChildren);
	}

	void IReader<IWidget>.Read(IJsonReader reader, out IWidget value)
	{
		value = null;
		ulong num = reader.ReadArrayBegin();
		if (num != 0)
		{
			reader.ReadArrayElement(0uL);
			PathSegment path = default(PathSegment);
			path.Read(reader);
			value = ContainerExtensions.FindChild(m_LastChildren, path);
			for (ulong num2 = 1uL; num2 < num; num2++)
			{
				reader.ReadArrayElement(num2);
				path.Read(reader);
				value = value.FindChild(path);
			}
		}
		reader.ReadArrayEnd();
	}
}
