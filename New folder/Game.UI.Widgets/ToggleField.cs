using System;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class ToggleField : Field<bool>, IWarning
{
	private bool m_Warning;

	[CanBeNull]
	public Func<bool> warningAction { get; set; }

	public bool warning
	{
		get
		{
			return m_Warning;
		}
		set
		{
			warningAction = null;
			m_Warning = value;
		}
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (warningAction != null)
		{
			bool flag = warningAction();
			if (flag != m_Warning)
			{
				m_Warning = flag;
				widgetChanges |= WidgetChanges.Properties;
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("warning");
		writer.Write(warning);
	}
}
