using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Tooltip;

public abstract class LabelIconTooltip : IconTooltip
{
	[CanBeNull]
	private LocalizedString m_Label;

	[CanBeNull]
	public LocalizedString label
	{
		get
		{
			return m_Label;
		}
		set
		{
			if (!object.Equals(value, m_Label))
			{
				m_Label = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("label");
		writer.Write(label);
	}
}
