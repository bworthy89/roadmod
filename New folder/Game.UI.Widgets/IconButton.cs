using System;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public class IconButton : Widget, ITooltipTarget, IInvokable, IWidget, IJsonWritable
{
	private bool m_Selected;

	private string m_Icon;

	public string icon
	{
		get
		{
			return m_Icon;
		}
		set
		{
			if (m_Icon != value)
			{
				m_Icon = value;
				SetPropertiesChanged();
			}
		}
	}

	public Action action { get; set; }

	[CanBeNull]
	public Func<bool> selected { get; set; }

	[CanBeNull]
	public LocalizedString? tooltip { get; set; }

	public void Invoke()
	{
		action();
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		bool flag = selected != null && selected();
		if (flag != m_Selected)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Selected = flag;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("icon");
		writer.Write(icon);
		writer.PropertyName("selected");
		writer.Write(m_Selected);
		writer.PropertyName("tooltip");
		writer.Write(tooltip);
	}
}
