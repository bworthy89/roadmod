using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Reflection;

namespace Game.UI.Widgets;

public class ExpandableGroup : NamedWidgetWithTooltip, IExpandable, IContainerWidget
{
	private ITypedValueAccessor<bool> m_ExpandedAccessor;

	private bool m_Expanded;

	private IList<IWidget> m_Children = Array.Empty<IWidget>();

	public virtual bool expanded
	{
		get
		{
			return m_ExpandedAccessor.GetTypedValue();
		}
		set
		{
			m_ExpandedAccessor.SetTypedValue(value);
		}
	}

	public IList<IWidget> children
	{
		get
		{
			return m_Children;
		}
		set
		{
			if (!object.Equals(value, m_Children))
			{
				ContainerExtensions.SetDefaults(value);
				m_Children = value;
				if (expanded)
				{
					SetChildrenChanged();
				}
			}
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
			return children;
		}
	}

	public ExpandableGroup(ITypedValueAccessor<bool> expandedAccessor)
	{
		m_ExpandedAccessor = expandedAccessor;
		m_Expanded = expandedAccessor.GetTypedValue();
	}

	public ExpandableGroup(bool expanded = false)
	{
		m_ExpandedAccessor = new ObjectAccessor<bool>(expanded, readOnly: false);
		m_Expanded = expanded;
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		bool typedValue = m_ExpandedAccessor.GetTypedValue();
		if (typedValue != m_Expanded)
		{
			widgetChanges |= WidgetChanges.Properties | WidgetChanges.Children;
			m_Expanded = typedValue;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("expanded");
		writer.Write(expanded);
	}
}
