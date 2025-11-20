using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Widgets;

namespace Game.UI.Tooltip;

public abstract class IconTooltip : Widget
{
	[CanBeNull]
	private string m_Icon;

	private TooltipColor m_Color;

	[CanBeNull]
	public string icon
	{
		get
		{
			return m_Icon;
		}
		set
		{
			if (value != m_Icon)
			{
				m_Icon = value;
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

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("icon");
		writer.Write(icon);
		writer.PropertyName("color");
		writer.Write((int)color);
	}
}
