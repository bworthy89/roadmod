using System;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class StringInputField : Field<string>, IWarning
{
	public static readonly int kDefaultMultilines = 5;

	public static readonly int kSingleLine = 0;

	private bool m_Warning;

	private int m_Multiline = kSingleLine;

	private int m_MaxLength;

	[CanBeNull]
	public Func<bool> warningAction { get; set; }

	public int multiline
	{
		get
		{
			return m_Multiline;
		}
		set
		{
			if (value != m_Multiline)
			{
				m_Multiline = value;
				SetPropertiesChanged();
			}
		}
	}

	public int maxLength
	{
		get
		{
			return m_MaxLength;
		}
		set
		{
			if (value != m_MaxLength)
			{
				m_MaxLength = value;
				SetPropertiesChanged();
			}
		}
	}

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

	public override string GetValue()
	{
		return base.GetValue() ?? string.Empty;
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
		writer.PropertyName("multiline");
		writer.Write(m_Multiline);
		writer.PropertyName("maxLength");
		writer.Write(m_MaxLength);
		writer.PropertyName("warning");
		writer.Write(warning);
	}
}
