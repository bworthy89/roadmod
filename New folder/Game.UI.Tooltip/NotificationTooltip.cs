using Colossal.UI.Binding;
using Game.Notifications;
using Game.UI.Widgets;

namespace Game.UI.Tooltip;

public class NotificationTooltip : Widget
{
	private string m_Name;

	private TooltipColor m_Color;

	private bool m_Verbose;

	public string name
	{
		get
		{
			return m_Name;
		}
		set
		{
			if (value != m_Name)
			{
				m_Name = value;
				SetPropertiesChanged();
			}
		}
	}

	public TooltipColor color
	{
		get
		{
			return m_Color;
		}
		set
		{
			if (value != m_Color)
			{
				m_Color = value;
				SetPropertiesChanged();
			}
		}
	}

	public bool verbose
	{
		get
		{
			return m_Verbose;
		}
		set
		{
			if (value != m_Verbose)
			{
				m_Verbose = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("name");
		writer.Write(name);
		writer.PropertyName("color");
		writer.Write((int)color);
		writer.PropertyName("verbose");
		writer.Write(verbose);
	}

	public static TooltipColor GetColor(IconPriority iconPriority)
	{
		if ((int)iconPriority >= 200)
		{
			return TooltipColor.Error;
		}
		if ((int)iconPriority >= 50)
		{
			return TooltipColor.Warning;
		}
		return TooltipColor.Info;
	}
}
