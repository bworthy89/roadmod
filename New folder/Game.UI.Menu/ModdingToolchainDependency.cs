using System;
using System.Collections.Generic;
using System.Linq;
using Game.Modding.Toolchain;
using Game.Reflection;
using Game.UI.Widgets;

namespace Game.UI.Menu;

public class ModdingToolchainDependency : ReadonlyField<IToolchainDependency>, IExpandable
{
	private bool m_Expanded;

	private IList<IWidget> m_Children = Array.Empty<IWidget>();

	public ITypedValueAccessor<bool> expandedAccessor { get; set; }

	public bool expanded
	{
		get
		{
			return expandedAccessor?.GetTypedValue() ?? true;
		}
		set
		{
			expandedAccessor?.SetTypedValue(value);
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

	public ModdingToolchainDependency()
	{
		base.valueWriter = new ModdingToolchainDependencyWriter();
	}

	public override WidgetChanges UpdateVisibility()
	{
		foreach (Widget item in visibleChildren.OfType<Widget>())
		{
			item.UpdateVisibility();
		}
		return base.UpdateVisibility();
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		bool flag = expandedAccessor?.GetTypedValue() ?? true;
		if (flag != m_Expanded)
		{
			widgetChanges |= WidgetChanges.Properties | WidgetChanges.Children;
			m_Expanded = flag;
		}
		return widgetChanges;
	}
}
