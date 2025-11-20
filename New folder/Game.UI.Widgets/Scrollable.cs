using System.Collections.Generic;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class Scrollable : LayoutContainer
{
	public Direction direction { get; set; }

	public Scrollable()
	{
		base.flex = FlexLayout.Fill;
	}

	public static Scrollable WithChildren(IList<IWidget> children)
	{
		return new Scrollable
		{
			children = children
		};
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("direction");
		writer.Write((int)direction);
	}
}
