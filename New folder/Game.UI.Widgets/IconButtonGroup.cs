using System;
using System.Collections.Generic;

namespace Game.UI.Widgets;

public class IconButtonGroup : Widget
{
	private IconButton[] m_Children = Array.Empty<IconButton>();

	public IconButton[] children
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

	public override IList<IWidget> visibleChildren => children;

	public static IconButtonGroup WithChildren(IconButton[] children)
	{
		return new IconButtonGroup
		{
			children = children
		};
	}
}
