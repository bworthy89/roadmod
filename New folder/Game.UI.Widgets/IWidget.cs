using System.Collections.Generic;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public interface IWidget : IJsonWritable
{
	PathSegment path { get; set; }

	IList<IWidget> visibleChildren { get; }

	string propertiesTypeName { get; }

	WidgetChanges Update();

	void WriteProperties(IJsonWriter writer);
}
