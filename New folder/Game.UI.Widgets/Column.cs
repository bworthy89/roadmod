using System.Collections.Generic;

namespace Game.UI.Widgets;

internal class Column : LayoutContainer
{
	public static Column WithChildren(IList<IWidget> children)
	{
		return new Column
		{
			children = children
		};
	}
}
