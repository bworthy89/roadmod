using System.Collections.Generic;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

internal class Row : LayoutContainer
{
	public bool wrap { get; set; }

	public static Row WithChildren(IList<IWidget> children)
	{
		return new Row
		{
			children = children
		};
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("wrap");
		writer.Write(wrap);
	}
}
