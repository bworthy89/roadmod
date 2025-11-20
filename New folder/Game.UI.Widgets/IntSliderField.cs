using System;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class IntSliderField : IntField<int>, IWarning
{
	private bool m_Warning;

	[CanBeNull]
	public Func<bool> warningAction { get; set; }

	[CanBeNull]
	public string unit { get; set; }

	public bool signed { get; set; }

	public bool separateThousands { get; set; }

	public bool scaleDragVolume { get; set; }

	public bool updateOnDragEnd { get; set; }

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
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.PropertyName("signed");
		writer.Write(signed);
		writer.PropertyName("separateThousands");
		writer.Write(separateThousands);
		writer.PropertyName("scaleDragVolume");
		writer.Write(scaleDragVolume);
		writer.PropertyName("updateOnDragEnd");
		writer.Write(updateOnDragEnd);
		writer.PropertyName("warning");
		writer.Write(warning);
	}
}
