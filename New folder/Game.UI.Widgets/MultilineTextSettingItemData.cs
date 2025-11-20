using Game.Settings;
using Game.UI.Menu;

namespace Game.UI.Widgets;

public class MultilineTextSettingItemData : AutomaticSettings.SettingItemData
{
	public string icon { get; set; }

	public MultilineTextSettingItemData(Setting setting, AutomaticSettings.IProxyProperty property, string prefix)
		: base(AutomaticSettings.WidgetType.MultilineText, setting, property, prefix)
	{
		SettingsUIMultilineTextAttribute attribute = property.GetAttribute<SettingsUIMultilineTextAttribute>();
		icon = ((attribute != null) ? attribute.icon : string.Empty);
	}

	protected override IWidget GetWidget()
	{
		return new MultilineText
		{
			path = base.path,
			displayName = base.displayName,
			displayNameAction = base.dispayNameAction,
			icon = icon,
			hidden = base.hideAction,
			disabled = base.disableAction
		};
	}
}
