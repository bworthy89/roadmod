using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Tooltip;

public class StringTooltip : IconTooltip
{
	private LocalizedString m_Value;

	public LocalizedString value
	{
		get
		{
			return m_Value;
		}
		set
		{
			if (!object.Equals(value, m_Value))
			{
				m_Value = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("value");
		writer.Write(value);
	}
}
