using System;
using Colossal.UI.Binding;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class StringInputFieldWithError : StringInputField
{
	private bool m_Error;

	public Func<bool> error { get; set; }

	public LocalizedString errorMessage { get; set; }

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (error != null)
		{
			bool flag = error();
			if (m_Error != flag)
			{
				widgetChanges |= WidgetChanges.Properties;
			}
			m_Error = flag;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("error");
		writer.Write(m_Error);
		writer.PropertyName("errorMessage");
		writer.Write(errorMessage);
	}
}
