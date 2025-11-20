using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.UI.Widgets;

public class ButtonRow : Widget
{
	private Button[] m_Children = Array.Empty<Button>();

	public Button[] children
	{
		get
		{
			return m_Children;
		}
		set
		{
			if (value != m_Children)
			{
				ContainerExtensions.SetDefaults(value);
				m_Children = value;
				SetChildrenChanged();
			}
		}
	}

	public override bool isVisible => children.Any((Button c) => c.isVisible);

	public override bool isActive => children.Any((Button c) => c.isActive);

	public override IList<IWidget> visibleChildren => children;

	public static ButtonRow WithChildren(Button[] children)
	{
		return new ButtonRow
		{
			children = children
		};
	}

	public override WidgetChanges UpdateVisibility()
	{
		WidgetChanges widgetChanges = base.UpdateVisibility();
		foreach (Widget item in visibleChildren.OfType<Widget>())
		{
			widgetChanges |= item.UpdateVisibility();
		}
		return widgetChanges;
	}
}
