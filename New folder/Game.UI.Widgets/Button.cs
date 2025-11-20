using System;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class Button : NamedWidgetWithTooltip, IInvokable, IWidget, IJsonWritable, IDisableCallback
{
	public Action action { get; set; }

	public bool showBackHint { get; set; }

	public void Invoke()
	{
		action();
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("showBackHint");
		writer.Write(showBackHint);
	}
}
