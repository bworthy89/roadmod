using System;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class DirectoryPickerButton : NamedWidgetWithTooltip, IInvokable, IWidget, IJsonWritable
{
	private string m_SelectedDirectory;

	public string displayValue { get; set; }

	public Action action { get; set; }

	public override string propertiesTypeName => "Game.UI.Widgets.DirectoryPickerButton";

	public void Invoke()
	{
		action();
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("selectedDirectory");
		writer.Write(m_SelectedDirectory);
		writer.PropertyName("displayValue");
		writer.Write(displayValue);
		writer.PropertyName("uiTag");
		writer.Write(base.uiTag);
	}
}
