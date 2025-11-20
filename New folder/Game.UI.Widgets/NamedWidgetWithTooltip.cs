using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public abstract class NamedWidgetWithTooltip : NamedWidget, ITooltipTarget, IUITagProvider
{
	[CanBeNull]
	public LocalizedString? tooltip { get; set; }

	[CanBeNull]
	public string uiTag { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("tooltip");
		writer.Write(tooltip);
		writer.PropertyName("uiTag");
		writer.Write(uiTag);
	}
}
