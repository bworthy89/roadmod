using System;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Menu;

public class DirectoryPickerField : Field<string>, IInvokable, IWidget, IJsonWritable, IWarning
{
	private bool m_Warning;

	[CanBeNull]
	public Func<bool> warningAction { get; set; }

	public override string propertiesTypeName => "Game.UI.Widgets.DirectoryPickerField";

	public Action action { get; set; }

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

	public void Invoke()
	{
		action?.Invoke();
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
