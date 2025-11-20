using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public class ImageField : Widget, ITooltipTarget
{
	public string m_URI;

	public LocalizedString m_Label;

	public LocalizedString? tooltip { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("uri");
		writer.Write(m_URI);
		writer.PropertyName("label");
		writer.Write(m_Label);
		writer.PropertyName("tooltip");
		writer.Write(tooltip);
	}
}
